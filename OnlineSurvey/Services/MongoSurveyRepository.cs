using MongoDB.Driver;
using OnlineSurvey.Models;

namespace OnlineSurvey.Services;

public sealed class MongoSurveyRepository : ISurveyRepository
{
    private readonly IMongoCollection<Survey> _surveys;
    private readonly IMongoCollection<SurveyResponse> _responses;

    public MongoSurveyRepository(IMongoDatabase database)
    {
        _surveys = database.GetCollection<Survey>("surveys");
        _responses = database.GetCollection<SurveyResponse>("responses");

        _surveys.Indexes.CreateOne(new CreateIndexModel<Survey>(
            Builders<Survey>.IndexKeys.Ascending(x => x.Slug),
            new CreateIndexOptions { Unique = true }));

        _responses.Indexes.CreateOne(new CreateIndexModel<SurveyResponse>(
            Builders<SurveyResponse>.IndexKeys
                .Ascending(x => x.SurveyId)
                .Descending(x => x.SubmittedAt)));
    }

    public Task<List<Survey>> GetAllAsync() =>
        _surveys.Find(Builders<Survey>.Filter.Empty)
            .SortByDescending(x => x.UpdatedAt)
            .ToListAsync();

    public async Task<Survey?> GetByIdAsync(string id) =>
        await _surveys.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<Survey?> GetPublishedBySlugAsync(string slug) =>
        await _surveys.Find(x => x.Slug == slug && x.Status == "Published")
            .FirstOrDefaultAsync();

    public Task InsertAsync(Survey survey) => _surveys.InsertOneAsync(survey);

    public Task UpdateAsync(Survey survey) =>
        _surveys.ReplaceOneAsync(x => x.Id == survey.Id, survey);

    public async Task DeleteAsync(string id)
    {
        await _surveys.DeleteOneAsync(x => x.Id == id);
        await _responses.DeleteManyAsync(x => x.SurveyId == id);
    }

    public Task InsertResponseAsync(SurveyResponse response) =>
        _responses.InsertOneAsync(response);

    public Task<List<SurveyResponse>> GetResponsesAsync(string surveyId) =>
        _responses.Find(x => x.SurveyId == surveyId)
            .SortByDescending(x => x.SubmittedAt)
            .ToListAsync();
}
