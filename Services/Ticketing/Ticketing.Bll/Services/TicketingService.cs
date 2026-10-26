using AutoMapper;
using Ticketing.Bll.Contracts;
using Ticketing.Bll.Specifications;
using Ticketing.Dal.UnitOfWork;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Exceptions;

namespace Ticketing.Bll.Services;

public sealed class TicketingService(IUnitOfWork unitOfWork, IMapper mapper) : ITicketingService
{
    public async Task<PagedResult<TicketTypeResponse>> GetTicketTypesAsync(
        TicketTypeQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
        {
            throw new ValidationException("Page must be positive and PageSize must be between 1 and 100.");
        }

        var page = await unitOfWork.TicketTypes.GetPageAsync(
            new TicketTypeFilterSpecification(query),
            cancellationToken);
        return new PagedResult<TicketTypeResponse>(
            mapper.Map<IReadOnlyList<TicketTypeResponse>>(page.Items),
            page.TotalCount,
            query.Page,
            query.PageSize);
    }

    public async Task<TicketTypeResponse> GetTicketTypeAsync(long id, CancellationToken cancellationToken)
    {
        var ticketType = await unitOfWork.TicketTypes.GetByIdAsync(id, cancellationToken);
        return mapper.Map<TicketTypeResponse>(ticketType
            ?? throw new EntityNotFoundException("Ticket type", id));
    }

    public async Task<TicketTypeResponse> CreateTicketTypeAsync(
        CreateTicketTypeRequest request,
        CancellationToken cancellationToken)
    {
        if (await unitOfWork.TicketTypes.ExistsByEventAndNameAsync(
                request.EventPublicId,
                request.Name.Trim(),
                cancellationToken))
        {
            throw new ConflictException("A ticket type with this name already exists for the event.");
        }

        var ticketType = new TicketType(
            request.EventPublicId,
            request.EventTitleSnapshot,
            request.Name,
            request.Price,
            request.TotalQuantity,
            request.Currency,
            request.Description);
        await unitOfWork.TicketTypes.AddAsync(ticketType, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapper.Map<TicketTypeResponse>(ticketType);
    }

    public async Task ChangeTicketTypePriceAsync(long id, decimal price, CancellationToken cancellationToken)
    {
        var ticketType = await unitOfWork.TicketTypes.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException("Ticket type", id);
        ticketType.ChangePrice(price);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ReservationResponse> CreateReservationAsync(
        CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.UserPublicId == Guid.Empty)
        {
            throw new ValidationException("User ID is required.");
        }

        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var reservation = new Reservation(request.UserPublicId, DateTimeOffset.UtcNow.AddMinutes(15));
            reservation.AddDetails(new ReservationDetails(
                request.Contact.Name,
                request.Contact.Email,
                request.Contact.Notes));

            foreach (var requestedItem in request.Items.OrderBy(item => item.TicketTypeId))
            {
                var ticketType = await unitOfWork.TicketTypes.GetForUpdateAsync(
                    requestedItem.TicketTypeId,
                    token) ?? throw new EntityNotFoundException("Ticket type", requestedItem.TicketTypeId);

                if (ticketType.IsDeleted)
                {
                    throw new EntityNotFoundException("Ticket type", requestedItem.TicketTypeId);
                }

                ticketType.Reserve(requestedItem.Quantity);
                reservation.AddItem(new ReservationItem(ticketType, requestedItem.Quantity));
            }

            await unitOfWork.Reservations.AddAsync(reservation, token);
            await unitOfWork.SaveChangesAsync(token);
            return mapper.Map<ReservationResponse>(reservation);
        }, cancellationToken);
    }

    public async Task<ReservationResponse> GetReservationAsync(long id, CancellationToken cancellationToken)
    {
        var reservation = await unitOfWork.EagerReservations.GetWithDetailsAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException("Reservation", id);
        return mapper.Map<ReservationResponse>(reservation);
    }

    public async Task<ReservationResponse> GetReservationExplicitAsync(long id, CancellationToken cancellationToken)
    {
        var reservation = await unitOfWork.ExplicitReservations.GetWithItemsAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException("Reservation", id);
        return mapper.Map<ReservationResponse>(reservation);
    }

    public async Task<IReadOnlyList<TicketTypeResponse>> GetReservationTicketTypesAsync(
        long id,
        CancellationToken cancellationToken)
    {
        var ticketTypes = await unitOfWork.ReservationTicketTypes.GetForReservationAsync(id, cancellationToken);
        if (ticketTypes.Count == 0 && !await unitOfWork.Reservations.ExistsAsync(id, cancellationToken))
        {
            throw new EntityNotFoundException("Reservation", id);
        }

        return mapper.Map<IReadOnlyList<TicketTypeResponse>>(ticketTypes);
    }

    public Task ConfirmReservationAsync(long id, CancellationToken cancellationToken)
        => unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var reservation = await unitOfWork.EagerReservations.GetWithDetailsAsync(id, token)
                ?? throw new EntityNotFoundException("Reservation", id);
            reservation.Confirm();
            foreach (var item in reservation.Items)
            {
                item.TicketType.SellReserved(item.Quantity);
                item.IssueTickets();
            }

            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

    public Task CancelReservationAsync(long id, CancellationToken cancellationToken)
        => unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var reservation = await unitOfWork.EagerReservations.GetWithDetailsAsync(id, token)
                ?? throw new EntityNotFoundException("Reservation", id);
            reservation.Cancel();
            foreach (var item in reservation.Items)
            {
                item.TicketType.ReleaseReservation(item.Quantity);
            }

            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

    public async Task UseTicketAsync(Guid publicId, CancellationToken cancellationToken)
    {
        var ticket = await unitOfWork.Tickets.GetByPublicIdAsync(publicId, cancellationToken)
            ?? throw new EntityNotFoundException("Ticket", publicId);
        ticket.MarkUsed();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
