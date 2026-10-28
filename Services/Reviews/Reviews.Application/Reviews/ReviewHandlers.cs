using AutoMapper;
using MongoDB.Bson;
using Reviews.Application.Abstractions;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Reviews;

public sealed class CreateReviewHandler(IReviewRepository repository, IReviewQueries queries, IMapper mapper)
    : MediatR.IRequestHandler<CreateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        if (await queries.ExistsForUserAndEventAsync(request.UserPublicId, request.EventPublicId, cancellationToken))
            throw new ConflictException("This user has already reviewed the event.");

        var review = new Review(request.EventPublicId, request.EventTitleSnapshot, request.TicketPublicId,
            request.UserPublicId, new Author(request.DisplayName, request.AvatarUrl, request.AuthorEmail), request.Rating,
            request.Text, request.Photos);
        await repository.AddAsync(review, cancellationToken);
        return mapper.Map<ReviewDto>(review);
    }
}

public sealed class UpdateReviewHandler(IReviewRepository repository)
    : MediatR.IRequestHandler<UpdateReviewCommand>
{
    public async Task Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await repository.GetByIdAsync(ObjectId.Parse(request.Id), cancellationToken)
            ?? throw new NotFoundException($"Review '{request.Id}' was not found.");
        review.Update(request.Rating, request.Text, request.Photos);
        await repository.ReplaceAsync(review, request.ExpectedVersion, cancellationToken);
    }
}

public sealed class DeleteReviewHandler(IReviewRepository repository)
    : MediatR.IRequestHandler<DeleteReviewCommand>
{
    public async Task Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
        => await repository.DeleteAsync(ObjectId.Parse(request.Id), request.ExpectedVersion, cancellationToken);
}

public sealed class GetReviewByIdHandler(IReviewQueries queries)
    : MediatR.IRequestHandler<GetReviewByIdQuery, ReviewDto>
{
    public async Task<ReviewDto> Handle(GetReviewByIdQuery request, CancellationToken cancellationToken)
        => await queries.GetByIdAsync(ObjectId.Parse(request.Id), cancellationToken)
            ?? throw new NotFoundException($"Review '{request.Id}' was not found.");
}

public sealed class GetReviewsListHandler(IReviewQueries queries)
    : MediatR.IRequestHandler<GetReviewsListQuery, IReadOnlyList<ReviewDto>>
{
    public Task<IReadOnlyList<ReviewDto>> Handle(GetReviewsListQuery request, CancellationToken cancellationToken)
        => queries.GetListAsync(new ReviewListFilter(request.EventPublicId, request.Search,
            request.Page, request.PageSize), cancellationToken);
}
