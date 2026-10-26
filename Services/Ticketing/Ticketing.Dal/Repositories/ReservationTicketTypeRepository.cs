using Microsoft.EntityFrameworkCore;
using Ticketing.Dal.Persistence;
using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Repositories;

public sealed class ReservationTicketTypeRepository(TicketingDbContext context)
    : IReservationTicketTypeRepository
{
    public async Task<IReadOnlyList<TicketType>> GetForReservationAsync(
        long reservationId,
        CancellationToken cancellationToken = default)
        => await context.ReservationItems
            .Where(item => item.ReservationId == reservationId)
            .Select(item => item.TicketType)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
}
