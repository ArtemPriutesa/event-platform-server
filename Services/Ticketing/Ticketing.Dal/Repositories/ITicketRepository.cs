using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Repositories;

public interface ITicketRepository : IGenericRepository<Ticket>
{
    Task<Ticket?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);
}
