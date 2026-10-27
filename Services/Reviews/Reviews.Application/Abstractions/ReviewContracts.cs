using MongoDB.Bson;
using Reviews.Domain.Entities;

namespace Reviews.Application.Abstractions;

public interface IReviewRepository
{
    Task AddAsync(Review review, CancellationToken cancellationToken);
    Task<Review?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken);
    Task ReplaceAsync(Review review, long expectedVersion, CancellationToken cancellationToken);
    Task DeleteAsync(ObjectId id, long expectedVersion, CancellationToken cancellationToken);
}

public interface IReviewQueries
{
    Task<ReviewDto?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReviewDto>> GetListAsync(ReviewListFilter filter, CancellationToken cancellationToken);
    Task<bool> ExistsForUserAndEventAsync(string userPublicId, string eventPublicId, CancellationToken cancellationToken);
}

public sealed record ReviewListFilter(string? EventPublicId, string? Search, int Page, int PageSize);

public sealed record ReviewDto(string Id, string EventPublicId, string EventTitleSnapshot,
    string TicketPublicId, string UserPublicId, string Author, string? AvatarUrl, string? AuthorEmail,
    int Rating, string Text, IReadOnlyList<string> Photos, IReadOnlyList<ReviewCommentDto> Comments,
    string? DiscussionId, DateTime CreatedAt, DateTime UpdatedAt, long Version);

public sealed record ReviewCommentDto(string Author, string? AvatarUrl, string Text, DateTime CreatedAt);
