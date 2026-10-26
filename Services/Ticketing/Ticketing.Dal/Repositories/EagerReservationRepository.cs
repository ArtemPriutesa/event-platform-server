using Microsoft.EntityFrameworkCore;
using Ticketing.Dal.Persistence;
using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Repositories;

public sealed class EagerReservationRepository(TicketingDbContext context)
    : IEagerReservationRepository
{
    public Task<Reservation?> GetWithDetailsAsync(long id, CancellationToken cancellationToken = default)
        => context.Reservations
            .Include(reservation => reservation.Details)
            .Include(reservation => reservation.Items)
                .ThenInclude(item => item.TicketType)
            .Include(reservation => reservation.Items)
                .ThenInclude(item => item.Tickets)
            .FirstOrDefaultAsync(reservation => reservation.Id == id, cancellationToken);
}
