using OnlineSurvey.Models;

namespace OnlineSurvey.Services;

public interface ISurveyRepository
{
    Task<List<Survey>> GetAllAsync();
    Task<Survey?> GetByIdAsync(string id);
    Task<Survey?> GetPublishedBySlugAsync(string slug);
    Task InsertAsync(Survey survey);
    Task UpdateAsync(Survey survey);
    Task DeleteAsync(string id);
    Task InsertResponseAsync(SurveyResponse response);
    Task<List<SurveyResponse>> GetResponsesAsync(string surveyId);
}
