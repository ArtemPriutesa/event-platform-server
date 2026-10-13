using Npgsql;

namespace EventCatalog.Dal.Connection;

public interface IDbSession : IAsyncDisposable
{
    NpgsqlTransaction? CurrentTransaction { get; }
    Task<NpgsqlConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
