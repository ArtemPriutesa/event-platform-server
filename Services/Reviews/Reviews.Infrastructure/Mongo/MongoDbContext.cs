using Microsoft.Extensions.Options;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
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
        settings.ClusterConfigurator = cluster => cluster.Subscribe(new DiagnosticsActivityEventSubscriber());
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
                var domainConstructor = typeof(Author).GetConstructors(BindingFlags.Instance | BindingFlags.Public)
                    .Single(constructor => constructor.GetParameters().Length == 3);
                map.UnmapConstructor(domainConstructor);
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
                var domainConstructor = typeof(Review).GetConstructors(BindingFlags.Instance | BindingFlags.Public)
                    .Single(constructor => constructor.GetParameters().Length == 9);
                map.UnmapConstructor(domainConstructor);
                var persistenceConstructor = typeof(Review).GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null)!;
                map.MapConstructor(persistenceConstructor);
                map.UnmapMember(x => x.Comments);
                map.MapField("_comments").SetElementName("comments");
            });
            _mappingsRegistered = true;
        }
    }
}
