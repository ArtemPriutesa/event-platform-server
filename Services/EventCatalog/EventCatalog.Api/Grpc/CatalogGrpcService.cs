using EventCatalog.Bll.Events;
using Grpc.Core;
using Platform.Contracts;

namespace EventCatalog.Api.Grpc;

public sealed class CatalogGrpcService(IEventService events) : CatalogGrpc.CatalogGrpcBase
{
    public override async Task<EventReply> GetEvent(GetEventRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "id must be greater than zero."));

        var item = await events.GetByIdAsync(request.Id, context.CancellationToken);
        if (item is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Event {request.Id} was not found."));

        return new EventReply
        {
            Id = item.Id,
            PublicId = item.PublicId.ToString(),
            Title = item.Title,
            Status = item.Status,
            StartsAt = item.StartsAt.ToString("O"),
            EndsAt = item.EndsAt.ToString("O")
        };
    }
}
