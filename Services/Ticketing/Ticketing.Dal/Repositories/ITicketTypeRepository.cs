using Ticketing.Domain.Entities;
using Ticketing.Domain.Specifications;

namespace Ticketing.Dal.Repositories;

public sealed record RepositoryPage<T>(IReadOnlyList<T> Items, int TotalCount);

public interface ITicketTypeRepository : IGenericRepository<TicketType>
{
    Task<bool> ExistsByEventAndNameAsync(Guid eventPublicId, string name, CancellationToken cancellationToken = default);
    Task<TicketType?> GetForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<RepositoryPage<TicketType>> GetPageAsync(
        Specification<TicketType> specification,
        CancellationToken cancellationToken = default);
}
