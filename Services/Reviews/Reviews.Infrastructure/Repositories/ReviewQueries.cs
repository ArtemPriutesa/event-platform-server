using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Application.Abstractions;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Mongo;

namespace Reviews.Infrastructure.Repositories;

public sealed class ReviewQueries(MongoDbContext context) : IReviewQueries
{
    public async Task<ReviewDto?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken)
        => await context.Reviews.Find(x => x.Id == id).Project(Project()).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ReviewDto>> GetListAsync(ReviewListFilter filter, CancellationToken cancellationToken)
    {
        var builder = Builders<Review>.Filter;
        var criteria = new List<FilterDefinition<Review>>();
        if (!string.IsNullOrWhiteSpace(filter.EventPublicId))
            criteria.Add(builder.Eq(x => x.EventPublicId, filter.EventPublicId));
        if (!string.IsNullOrWhiteSpace(filter.Search))
            criteria.Add(builder.Text(filter.Search));

        var query = context.Reviews.Aggregate().Match(criteria.Count == 0 ? builder.Empty : builder.And(criteria));
        return await query.SortByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((filter.Page - 1) * filter.PageSize).Limit(filter.PageSize)
            .Project(Project()).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsForUserAndEventAsync(string userPublicId, string eventPublicId,
        CancellationToken cancellationToken)
        => context.Reviews.Find(x => x.UserPublicId == userPublicId && x.EventPublicId == eventPublicId)
            .AnyAsync(cancellationToken);

    private static ProjectionDefinition<Review, ReviewDto> Project()
        => Builders<Review>.Projection.Expression(review => new ReviewDto(
            review.Id.ToString(), review.EventPublicId, review.EventTitleSnapshot,
            review.TicketPublicId, review.UserPublicId, review.Author.DisplayName,
            review.Author.AvatarUrl, review.Author.Email == null ? null : review.Author.Email.Value,
            review.Rating, review.Text, review.Photos.ToArray(),
            review.Comments.Select(comment => new ReviewCommentDto(comment.Author.DisplayName,
                comment.Author.AvatarUrl, comment.Text, comment.CreatedAt)).ToArray(),
            review.DiscussionId, review.CreatedAt, review.UpdatedAt, review.Version));
}
