using Microsoft.EntityFrameworkCore;
using Ticketing.Dal.Persistence;
using Ticketing.Dal.Repositories;
using Ticketing.Domain.Exceptions;

namespace Ticketing.Dal.UnitOfWork;

public sealed class UnitOfWork(
    TicketingDbContext context,
    ITicketTypeRepository ticketTypes,
    IReservationRepository reservations,
    IEagerReservationRepository eagerReservations,
    IExplicitReservationRepository explicitReservations,
    IReservationTicketTypeRepository reservationTicketTypes,
    ITicketRepository tickets) : IUnitOfWork
{
    public ITicketTypeRepository TicketTypes { get; } = ticketTypes;
    public IReservationRepository Reservations { get; } = reservations;
    public IEagerReservationRepository EagerReservations { get; } = eagerReservations;
    public IExplicitReservationRepository ExplicitReservations { get; } = explicitReservations;
    public IReservationTicketTypeRepository ReservationTicketTypes { get; } = reservationTicketTypes;
    public ITicketRepository Tickets { get; } = tickets;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in context.ChangeTracker.Entries<Domain.Entities.AuditableEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                var version = entry.Property(entity => entity.Version);
                if (version.CurrentValue == version.OriginalValue)
                {
                    entry.Entity.MarkUpdated(DateTimeOffset.UtcNow);
                }
            }
        }

        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The record was changed by another request. Reload it and retry.");
        }
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var result = await operation(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
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
