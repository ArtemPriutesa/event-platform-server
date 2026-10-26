using Microsoft.EntityFrameworkCore;
using Ticketing.Dal.Persistence;
using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Repositories;

public class GenericRepository<T>(TicketingDbContext context) : IGenericRepository<T>
    where T : class, IEntity
{
    protected readonly TicketingDbContext Context = context;
    protected readonly DbSet<T> Set = context.Set<T>();

    public Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Set.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await Set.AddAsync(entity, cancellationToken);

    public void Update(T entity) => Set.Update(entity);

    public void Delete(T entity)
    {
        if (entity is AuditableEntity auditableEntity)
        {
            auditableEntity.SoftDelete();
            Set.Update(entity);
            return;
        }

        Set.Remove(entity);
    }
}
