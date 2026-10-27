using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Application.Abstractions;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Infrastructure.Mongo;

namespace Reviews.Infrastructure.Repositories;

public sealed class ReviewRepository(MongoDbContext context) : IReviewRepository
{
    public async Task AddAsync(Review review, CancellationToken cancellationToken)
    {
        try { await context.Reviews.InsertOneAsync(review, cancellationToken: cancellationToken); }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        { throw new ConflictException("A review already exists for this user and event or ticket."); }
    }

    public async Task<Review?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken)
        => await context.Reviews.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task ReplaceAsync(Review review, long expectedVersion, CancellationToken cancellationToken)
    {
        var result = await context.Reviews.ReplaceOneAsync(
            x => x.Id == review.Id && x.Version == expectedVersion,
            review,
            cancellationToken: cancellationToken);
        if (result.MatchedCount == 0)
            throw new ConflictException("The review has changed since it was read. Reload it and retry.");
    }

    public async Task DeleteAsync(ObjectId id, long expectedVersion, CancellationToken cancellationToken)
    {
        var result = await context.Reviews.DeleteOneAsync(
            x => x.Id == id && x.Version == expectedVersion, cancellationToken);
        if (result.DeletedCount > 0) return;
        var exists = await context.Reviews.Find(x => x.Id == id).AnyAsync(cancellationToken);
        if (!exists) throw new NotFoundException($"Review '{id}' was not found.");
        throw new ConflictException("The review has changed since it was read. Reload it and retry.");
    }
}
