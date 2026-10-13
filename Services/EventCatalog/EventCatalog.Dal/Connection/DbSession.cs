using Npgsql;

namespace EventCatalog.Dal.Connection;

public sealed class DbSession(NpgsqlDataSource dataSource) : IDbSession
{
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;

    public NpgsqlTransaction? CurrentTransaction => _transaction;

    public async Task<NpgsqlConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is null)
        {
            _connection = await dataSource.OpenConnectionAsync(cancellationToken);
        }

        return _connection;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active for this unit of work.");
        }

        var connection = await GetConnectionAsync(cancellationToken);
        _transaction = await connection.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        var transaction = _transaction
            ?? throw new InvalidOperationException("There is no active transaction to commit.");

        await transaction.CommitAsync(cancellationToken);
        await transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        var transaction = _transaction
            ?? throw new InvalidOperationException("There is no active transaction to roll back.");

        await transaction.RollbackAsync(cancellationToken);
        await transaction.DisposeAsync();
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
