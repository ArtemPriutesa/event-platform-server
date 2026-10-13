using System.Runtime.ExceptionServices;
using EventCatalog.Dal.Connection;
using EventCatalog.Dal.Events;
using EventCatalog.Dal.Repositories;
using EventCatalog.Domain.Catalog;

namespace EventCatalog.Dal.UnitOfWork;

public sealed class UnitOfWork(
    IDbSession session,
    IEventRepository events,
    ICategoryRepository categories,
    IOrganizerRepository organizers,
    ITagRepository tags) : IUnitOfWork
{
    public IEventRepository Events { get; } = events;
    public ICategoryRepository Categories { get; } = categories;
    public IOrganizerRepository Organizers { get; } = organizers;
    public ITagRepository Tags { get; } = tags;

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => session.BeginTransactionAsync(cancellationToken);

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => session.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => session.RollbackAsync(cancellationToken);

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        await BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception operationException)
        {
            try
            {
                await RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                throw new AggregateException(
                    "The unit-of-work operation failed and its transaction could not be rolled back.",
                    operationException,
                    rollbackException);
            }

            ExceptionDispatchInfo.Capture(operationException).Throw();
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async token =>
        {
            await operation(token);
            return true;
        }, cancellationToken);
    }
}
