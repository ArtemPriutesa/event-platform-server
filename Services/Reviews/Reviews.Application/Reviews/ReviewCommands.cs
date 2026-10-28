using Reviews.Application.Abstractions;

namespace Reviews.Application.Reviews;

public sealed record CreateReviewCommand(string EventPublicId, string EventTitleSnapshot,
    string TicketPublicId, string UserPublicId, string DisplayName, string? AvatarUrl, string? AuthorEmail,
    int Rating, string Text, IReadOnlyList<string>? Photos) : ICommand<ReviewDto>;

public sealed record UpdateReviewCommand(string Id, long ExpectedVersion,
    int Rating, string Text, IReadOnlyList<string>? Photos) : ICommand;

public sealed record DeleteReviewCommand(string Id, long ExpectedVersion) : ICommand;

public sealed record GetReviewByIdQuery(string Id) : IQuery<ReviewDto>;

public sealed record GetReviewsListQuery(string? EventPublicId, string? Search,
    int Page = 1, int PageSize = 20) : IQuery<IReadOnlyList<ReviewDto>>;
