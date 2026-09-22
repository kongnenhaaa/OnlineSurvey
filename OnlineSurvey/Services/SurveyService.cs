using System.Security.Cryptography;
using System.Text.Json;
using OnlineSurvey.Models;
using StackExchange.Redis;

namespace OnlineSurvey.Services;

public sealed class SurveyService
{
    private const int SubmissionLimit = 3;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RateWindow = TimeSpan.FromMinutes(1);
    private readonly ISurveyRepository _repository;
    private readonly IDatabase _redis;
    private readonly ILogger<SurveyService> _logger;

    public SurveyService(
        ISurveyRepository repository,
        IConnectionMultiplexer redis,
        ILogger<SurveyService> logger)
    {
        _repository = repository;
        _redis = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<Survey?> GetPublishedBySlugAsync(string slug)
    {
        slug = slug.Trim().ToLowerInvariant();
        var cacheKey = CacheKey(slug);

        try
        {
            var cached = await _redis.StringGetAsync(cacheKey);
            if (!cached.IsNullOrEmpty)
            {
                var cachedSurvey = JsonSerializer.Deserialize<Survey>(cached.ToString());
                if (cachedSurvey is not null)
                {
                    return cachedSurvey;
                }

                await _redis.KeyDeleteAsync(cacheKey);
            }
        }
        catch (RedisException exception)
        {
            _logger.LogWarning(exception, "Redis cache read failed for survey slug {Slug}", slug);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Invalid survey cache for slug {Slug}", slug);
            try { await _redis.KeyDeleteAsync(cacheKey); } catch (RedisException) { }
        }

        var survey = await _repository.GetPublishedBySlugAsync(slug);
        if (survey is not null)
        {
            try
            {
                await _redis.StringSetAsync(
                    cacheKey,
                    JsonSerializer.Serialize(survey),
                    CacheDuration);
            }
            catch (RedisException exception)
            {
                _logger.LogWarning(exception, "Redis cache write failed for survey slug {Slug}", slug);
            }
        }

        return survey;
    }

    public Task<Survey?> GetByIdAsync(string id) => _repository.GetByIdAsync(id);
    public Task<List<Survey>> GetAllAsync() => _repository.GetAllAsync();
    public Task InsertResponseAsync(SurveyResponse response) => _repository.InsertResponseAsync(response);

    public async Task CreateAsync(Survey survey)
    {
        await _repository.InsertAsync(survey);
        await InvalidateAsync(survey.Slug);
    }

    public async Task UpdateAsync(Survey survey, string? oldSlug = null)
    {
        await _repository.UpdateAsync(survey);
        await InvalidateAsync(oldSlug);
        await InvalidateAsync(survey.Slug);
    }

    public async Task DeleteAsync(Survey survey)
    {
        await _repository.DeleteAsync(survey.Id);
        await InvalidateAsync(survey.Slug);
    }

    public Task<List<SurveyResponse>> GetResponsesAsync(string surveyId) =>
        _repository.GetResponsesAsync(surveyId);

    public async Task<bool> AllowSubmissionAsync(string surveyId, string clientKey)
    {
        var key = $"rate:survey:{surveyId}:{clientKey}";
        try
        {
            var count = await _redis.StringIncrementAsync(key);

            if (count == 1)
            {
                await _redis.KeyExpireAsync(key, RateWindow);
            }

            return count <= SubmissionLimit;
        }
        catch (RedisException exception)
        {
            _logger.LogWarning(exception, "Redis rate limiter unavailable for survey {SurveyId}", surveyId);
            return true;
        }
    }

    public async Task InvalidateAsync(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return;
        }

        try
        {
            await _redis.KeyDeleteAsync(CacheKey(slug.Trim().ToLowerInvariant()));
        }
        catch (RedisException exception)
        {
            _logger.LogWarning(exception, "Redis cache invalidation failed for survey slug {Slug}", slug);
        }
    }

    public static string HashClient(string value)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes)[..32].ToLowerInvariant();
    }

    private static string CacheKey(string slug) => $"survey:published:{slug}";
}
