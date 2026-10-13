using EventCatalog.Dal.Events;
using EventCatalog.Dal.Repositories;
using EventCatalog.Domain.Catalog;

namespace EventCatalog.Dal.UnitOfWork;

public interface IUnitOfWork
{
    IEventRepository Events { get; }
    ICategoryRepository Categories { get; }
    IOrganizerRepository Organizers { get; }
    ITagRepository Tags { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
