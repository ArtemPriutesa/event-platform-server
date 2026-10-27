using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Reviews.Infrastructure.Mongo;

namespace Reviews.Infrastructure.Seeding;

public sealed class ReviewsStartupService(MongoDbContext context, ReviewDataSeeder seeder) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var reviews = context.Reviews;
        await reviews.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<Reviews.Domain.Entities.Review>(
                Builders<Reviews.Domain.Entities.Review>.IndexKeys.Ascending(x => x.EventPublicId)
                    .Descending(x => x.CreatedAt), new CreateIndexOptions { Name = "eventPublicId_1_createdAt_-1" }),
            new CreateIndexModel<Reviews.Domain.Entities.Review>(
                Builders<Reviews.Domain.Entities.Review>.IndexKeys.Text(x => x.Text),
                new CreateIndexOptions { Name = "review_text" }),
            new CreateIndexModel<Reviews.Domain.Entities.Review>(
                Builders<Reviews.Domain.Entities.Review>.IndexKeys.Ascending(x => x.EventPublicId)
                    .Ascending(x => x.UserPublicId), new CreateIndexOptions { Name = "eventPublicId_1_userPublicId_1", Unique = true }),
            new CreateIndexModel<Reviews.Domain.Entities.Review>(
                Builders<Reviews.Domain.Entities.Review>.IndexKeys.Ascending(x => x.TicketPublicId),
                new CreateIndexOptions { Name = "ticketPublicId_1", Unique = true })
        ], cancellationToken);
        await seeder.SeedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
