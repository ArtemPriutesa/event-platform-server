using Ticketing.Bll.Contracts;

namespace Ticketing.Bll.Services;

public interface ITicketingService
{
    Task<PagedResult<TicketTypeResponse>> GetTicketTypesAsync(TicketTypeQuery query, CancellationToken cancellationToken);
    Task<TicketTypeResponse> GetTicketTypeAsync(long id, CancellationToken cancellationToken);
    Task<TicketTypeResponse> CreateTicketTypeAsync(CreateTicketTypeRequest request, CancellationToken cancellationToken);
    Task ChangeTicketTypePriceAsync(long id, decimal price, CancellationToken cancellationToken);
    Task<ReservationResponse> CreateReservationAsync(CreateReservationRequest request, CancellationToken cancellationToken);
    Task<ReservationResponse> GetReservationAsync(long id, CancellationToken cancellationToken);
    Task<ReservationResponse> GetReservationExplicitAsync(long id, CancellationToken cancellationToken);
    Task<IReadOnlyList<TicketTypeResponse>> GetReservationTicketTypesAsync(long id, CancellationToken cancellationToken);
    Task ConfirmReservationAsync(long id, CancellationToken cancellationToken);
    Task CancelReservationAsync(long id, CancellationToken cancellationToken);
    Task UseTicketAsync(Guid publicId, CancellationToken cancellationToken);
}
