using Grpc.Core;
using Platform.Contracts;
using Ticketing.Bll.Contracts;
using Ticketing.Bll.Services;
using Ticketing.Domain.Exceptions;

namespace Ticketing.Api.Grpc;

public sealed class TicketingGrpcService(ITicketingService ticketing) : TicketingGrpc.TicketingGrpcBase
{
    public override async Task<ReservationReply> GetReservation(GetReservationRequest request, ServerCallContext context)
    {
        ReservationResponse reservation;
        try
        {
            reservation = await ticketing.GetReservationAsync(request.Id, context.CancellationToken);
        }
        catch (EntityNotFoundException exception)
        {
            throw new RpcException(new Status(StatusCode.NotFound, exception.Message));
        }
        catch (ValidationException exception)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, exception.Message));
        }
        catch (ConflictException exception)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, exception.Message));
        }

        var reply = new ReservationReply
        {
            Id = reservation.Id,
            PublicId = reservation.PublicId.ToString(),
            UserPublicId = reservation.UserPublicId.ToString(),
            Status = reservation.Status.ToString()
        };
        foreach (var item in reservation.Items)
        {
            var ticketType = await ticketing.GetTicketTypeAsync(item.TicketTypeId, context.CancellationToken);
            reply.Items.Add(new ReservationLine
            {
                EventPublicId = ticketType.EventPublicId.ToString(),
                EventTitle = item.EventTitleSnapshot,
                TicketType = item.TicketTypeNameSnapshot,
                Quantity = item.Quantity
            });
        }
        return reply;
    }
}
