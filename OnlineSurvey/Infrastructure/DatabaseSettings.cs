namespace OnlineSurvey.Infrastructure;

public sealed class MongoDbSettings
{
    public string ConnectionString { get; set; } = "";
    public string DatabaseName { get; set; } = "OnlineSurvey";
}

public sealed class RedisSettings
{
    public string ConnectionString { get; set; } = "localhost:6379";
}

public sealed class AuthDatabaseSettings
{
    public string DatabaseName { get; set; } = "OnlineSurvey";
}
