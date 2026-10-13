using EventCatalog.Domain.Repositories;
using EventCatalog.Domain.Catalog;

namespace EventCatalog.Dal.Repositories;

public interface IGenericRepository<T> where T : class, IEntity
{
    Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<long> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}

public interface IOrganizerRepository : IGenericRepository<Organizer>
{
    Task<Organizer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public interface ITagRepository : IGenericRepository<Tag>
{
    Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}
