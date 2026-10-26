using Microsoft.EntityFrameworkCore;
using Ticketing.Dal.Persistence;
using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Repositories;

public sealed class TicketRepository(TicketingDbContext context)
    : GenericRepository<Ticket>(context), ITicketRepository
{
    public Task<Ticket?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(ticket => ticket.PublicId == publicId && !ticket.IsDeleted, cancellationToken);
}
