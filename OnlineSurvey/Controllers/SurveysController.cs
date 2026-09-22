using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using OnlineSurvey.Models;
using OnlineSurvey.Services;

namespace OnlineSurvey.Controllers;

[Route("surveys")]
public sealed class SurveysController : Controller
{
    private readonly SurveyService _surveyService;

    public SurveysController(SurveyService surveyService)
    {
        _surveyService = surveyService;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var survey = await _surveyService.GetPublishedBySlugAsync(slug);
        return survey is null
            ? NotFound()
            : View(new SurveyPageViewModel { Survey = survey });
    }

    [HttpPost("{id}/submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(string id, SurveySubmitViewModel model)
    {
        model.Answers ??= new Dictionary<string, string[]>();
        var survey = await _surveyService.GetByIdAsync(id);
        if (survey is null || survey.Status != "Published")
        {
            return NotFound();
        }

        if (Request.Cookies.ContainsKey($"survey-submitted-{survey.Id}"))
        {
            return View("AlreadySubmitted", survey);
        }

        ValidateAnswers(survey, model.Answers);
        if (!ModelState.IsValid)
        {
            return View("Details", new SurveyPageViewModel
            {
                Survey = survey,
                SubmitModel = model
            });
        }

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        var clientKey = SurveyService.HashClient($"{remoteIp}|{userAgent}");

        if (!await _surveyService.AllowSubmissionAsync(survey.Id, clientKey))
        {
            return View("RateLimited", survey);
        }

        var response = new SurveyResponse
        {
            SurveyId = survey.Id,
            SurveyVersion = survey.Version,
            RespondentName = model.RespondentName?.Trim() ?? "",
            Answers = model.Answers
                .Where(x => x.Value is not null && x.Value.Length > 0)
                .Select(x => new SurveyAnswer
                {
                    QuestionId = x.Key,
                    Values = x.Value.Where(value => !string.IsNullOrWhiteSpace(value))
                        .Select(value => value.Trim())
                        .ToList()
                })
                .Where(x => x.Values.Count > 0)
                .ToList()
        };

        await _surveyService.InsertResponseAsync(response);
        Response.Cookies.Append(
            $"survey-submitted-{survey.Id}",
            "1",
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                MaxAge = TimeSpan.FromMinutes(10),
                SameSite = SameSiteMode.Lax
            });
        return View("Submitted", survey);
    }

    private void ValidateAnswers(Survey survey, Dictionary<string, string[]>? answers)
    {
        answers ??= new Dictionary<string, string[]>();
        var knownQuestionIds = survey.Questions.Select(question => question.Id).ToHashSet();

        foreach (var unknownQuestionId in answers.Keys.Where(key => !knownQuestionIds.Contains(key)))
        {
            ModelState.AddModelError(string.Empty, "Form chứa câu hỏi không hợp lệ.");
        }

        foreach (var question in survey.Questions.OrderBy(x => x.Order))
        {
            answers.TryGetValue(question.Id, out var values);
            values ??= [];
            var cleanValues = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();

            if (question.Required && cleanValues.Length == 0)
            {
                ModelState.AddModelError(
                    $"Answers[{question.Id}]",
                    "Vui lòng trả lời câu hỏi này.");
                continue;
            }

            if (cleanValues.Length == 0)
            {
                continue;
            }

            var validOptions = question.Options.Select(x => x.Id).ToHashSet();
            var valid = question.Type switch
            {
                "single_choice" => cleanValues.Length == 1 && validOptions.Contains(cleanValues[0]),
                "multiple_choice" => cleanValues.All(validOptions.Contains),
                "text" or "textarea" => cleanValues.Length == 1 && cleanValues[0].Length <= 5000,
                _ => cleanValues.Length <= 1
            };

            if (!valid)
            {
                ModelState.AddModelError(
                    $"Answers[{question.Id}]",
                    "Câu trả lời không hợp lệ.");
            }
        }
    }
}
