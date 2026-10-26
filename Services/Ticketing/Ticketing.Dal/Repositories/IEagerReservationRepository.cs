using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Repositories;

public interface IEagerReservationRepository
{
    Task<Reservation?> GetWithDetailsAsync(long id, CancellationToken cancellationToken = default);
}
