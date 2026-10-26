using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Repositories;

public interface IReservationTicketTypeRepository
{
    Task<IReadOnlyList<TicketType>> GetForReservationAsync(
        long reservationId,
        CancellationToken cancellationToken = default);
}
