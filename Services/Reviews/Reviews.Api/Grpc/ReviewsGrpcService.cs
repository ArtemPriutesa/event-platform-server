using Grpc.Core;
using MediatR;
using Platform.Contracts;
using Reviews.Application.Reviews;

namespace Reviews.Api.Grpc;

public sealed class ReviewsGrpcService(ISender sender) : ReviewsGrpc.ReviewsGrpcBase
{
    public override async Task<ReviewsReply> GetReviewsForEvent(GetReviewsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.EventPublicId, out _))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "event_public_id must be a UUID."));

        var reviews = await sender.Send(new GetReviewsListQuery(request.EventPublicId, null), context.CancellationToken);
        var reply = new ReviewsReply();
        reply.Reviews.AddRange(reviews.Select(review => new ReviewReply
        {
            Id = review.Id,
            EventPublicId = review.EventPublicId,
            Author = review.Author,
            Rating = review.Rating,
            Text = review.Text
        }));
        return reply;
    }
}
