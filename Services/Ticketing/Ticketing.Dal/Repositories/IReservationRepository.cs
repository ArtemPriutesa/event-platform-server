using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Repositories;

public interface IReservationRepository : IGenericRepository<Reservation>
{
    Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default);
}
