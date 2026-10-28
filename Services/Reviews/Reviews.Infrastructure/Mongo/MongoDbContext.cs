using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.Infrastructure.Mongo;

public sealed class MongoDbContext
{
    private static readonly object MappingLock = new();
    private static bool _mappingsRegistered;
    public IMongoDatabase Database { get; }
    public IMongoCollection<Review> Reviews => Database.GetCollection<Review>("reviews");

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        RegisterMappings();
        var settings = MongoClientSettings.FromConnectionString(options.Value.ConnectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        settings.MaxConnectionPoolSize = 100;
        Database = new MongoClient(settings).GetDatabase(options.Value.DatabaseName);
    }

    private static void RegisterMappings()
    {
        if (_mappingsRegistered) return;
        lock (MappingLock)
        {
            if (_mappingsRegistered) return;
            var conventions = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register("reviews-camel-case", conventions, _ => true);

            BsonClassMap.RegisterClassMap<BaseEntity>(map =>
            {
                map.AutoMap();
                map.MapIdMember(x => x.Id);
            });
            BsonClassMap.RegisterClassMap<Author>(map =>
            {
                map.AutoMap();
                map.MapCreator(x => new Author(x.DisplayName, x.AvatarUrl, x.Email == null ? null : x.Email.Value));
            });
            BsonClassMap.RegisterClassMap<Email>(map =>
            {
                map.AutoMap();
                map.MapCreator(x => new Email(x.Value));
            });
            BsonClassMap.RegisterClassMap<ReviewComment>(map =>
            {
                map.AutoMap();
                map.MapCreator(x => new ReviewComment(x.Author, x.Text));
            });
            BsonClassMap.RegisterClassMap<Review>(map =>
            {
                map.AutoMap();
                map.UnmapMember(x => x.Comments);
                map.UnmapMember(x => x.Photos);
                map.MapField("_comments").SetElementName("comments");
                map.MapField("_photos").SetElementName("photos");
            });
            _mappingsRegistered = true;
        }
    }
}
