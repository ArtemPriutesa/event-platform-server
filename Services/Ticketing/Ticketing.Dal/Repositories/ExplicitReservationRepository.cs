using Microsoft.EntityFrameworkCore;
using Ticketing.Dal.Persistence;
using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Repositories;

public sealed class ExplicitReservationRepository(TicketingDbContext context)
    : IExplicitReservationRepository
{
    public async Task<Reservation?> GetWithItemsAsync(long id, CancellationToken cancellationToken = default)
    {
        var reservation = await context.Reservations
            .Include(item => item.Details)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (reservation is null)
        {
            return null;
        }

        await context.Entry(reservation)
            .Collection(item => item.Items)
            .Query()
            .Include(item => item.TicketType)
            .Include(item => item.Tickets)
            .LoadAsync(cancellationToken);
        return reservation;
    }
}
