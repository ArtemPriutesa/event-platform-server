using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Mongo;

namespace Reviews.Infrastructure.Seeding;

public sealed class ReviewDataSeeder(MongoDbContext context)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await context.Reviews.EstimatedDocumentCountAsync(cancellationToken: cancellationToken) > 0) return;
        var reviews = new[]
        {
            new Review("A1000000-0000-4000-8000-000000000001", "Demo Concert",
                "D4000000-0000-4000-8000-000000000001", "B2000000-0000-4000-8000-000000000001",
                new Author("Demo Attendee", "https://example.invalid/avatars/demo.png"), 5,
                "Great live music and a welcoming venue.",
                ["https://example.invalid/events/demo/stage.jpg"], "F6000000-0000-4000-8000-000000000001"),
            new Review("A1000000-0000-4000-8000-000000000001", "Demo Concert",
                "D4000000-0000-4000-8000-000000000002", "B2000000-0000-4000-8000-000000000002",
                new Author("Another Attendee"), 4, "Excellent performance.")
        };
        reviews[0].AddComment(new Author("Event Organizer"), "Thank you for coming!");
        await context.Reviews.InsertManyAsync(reviews,
            new InsertManyOptions { IsOrdered = true }, cancellationToken);
    }
}
