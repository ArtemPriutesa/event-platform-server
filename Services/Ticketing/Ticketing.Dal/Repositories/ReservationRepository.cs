using Microsoft.EntityFrameworkCore;
using Ticketing.Dal.Persistence;
using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Repositories;

public sealed class ReservationRepository(TicketingDbContext context) : GenericRepository<Reservation>(context), IReservationRepository
{
    public Task<bool> ExistsAsync(long id, CancellationToken cancellationToken = default)
        => Set.AnyAsync(reservation => reservation.Id == id, cancellationToken);
}
