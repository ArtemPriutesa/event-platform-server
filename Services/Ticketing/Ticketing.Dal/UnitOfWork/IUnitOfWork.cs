using Ticketing.Dal.Repositories;

namespace Ticketing.Dal.UnitOfWork;

public interface IUnitOfWork
{
    ITicketTypeRepository TicketTypes { get; }
    IReservationRepository Reservations { get; }
    IEagerReservationRepository EagerReservations { get; }
    IExplicitReservationRepository ExplicitReservations { get; }
    IReservationTicketTypeRepository ReservationTicketTypes { get; }
    ITicketRepository Tickets { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
