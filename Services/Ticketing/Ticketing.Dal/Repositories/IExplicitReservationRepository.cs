using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Repositories;

public interface IExplicitReservationRepository
{
    Task<Reservation?> GetWithItemsAsync(long id, CancellationToken cancellationToken = default);
}
