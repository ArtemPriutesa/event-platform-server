namespace Reviews.Infrastructure.Mongo;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "event_platform_payments";
}
