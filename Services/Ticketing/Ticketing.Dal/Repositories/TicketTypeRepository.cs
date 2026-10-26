using Microsoft.EntityFrameworkCore;
using Ticketing.Dal.Persistence;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Specifications;

namespace Ticketing.Dal.Repositories;

public sealed class TicketTypeRepository(TicketingDbContext context)
    : GenericRepository<TicketType>(context), ITicketTypeRepository
{
    public Task<bool> ExistsByEventAndNameAsync(
        Guid eventPublicId,
        string name,
        CancellationToken cancellationToken = default)
        => Set.AnyAsync(ticketType =>
            ticketType.EventPublicId == eventPublicId
            && ticketType.Name == name
            && !ticketType.IsDeleted,
            cancellationToken);

    public async Task<TicketType?> GetForUpdateAsync(long id, CancellationToken cancellationToken = default)
    {
        if (Context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Ticket inventory must be locked inside a unit-of-work transaction.");
        }

        return await Set
            .FromSqlInterpolated(
                $"SELECT * FROM ticket_types WHERE id = {id} AND is_deleted = FALSE FOR UPDATE")
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<RepositoryPage<TicketType>> GetPageAsync(
        Specification<TicketType> specification,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TicketType> query = Set.AsNoTracking();
        foreach (var criteria in specification.Criteria)
        {
            query = query.Where(criteria);
        }

        foreach (var include in specification.Includes)
        {
            query = query.Include(include);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        if (specification.OrderBy is not null)
        {
            query = query.OrderBy(specification.OrderBy).ThenBy(ticketType => ticketType.Id);
        }
        else if (specification.OrderByDescending is not null)
        {
            query = query.OrderByDescending(specification.OrderByDescending).ThenBy(ticketType => ticketType.Id);
        }

        if (specification.Skip.HasValue)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        var items = await query.ToListAsync(cancellationToken);
        return new RepositoryPage<TicketType>(items, totalCount);
    }
}
