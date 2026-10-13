using Dapper;
using EventCatalog.Dal.Connection;

namespace EventCatalog.Dal.Repositories;

public abstract class BaseDapperRepository<T>(IDbSession session)
{
    protected async Task<IReadOnlyList<T>> QueryAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            parameters,
            session.CurrentTransaction,
            cancellationToken: cancellationToken);
        return (await connection.QueryAsync<T>(command)).AsList();
    }

    protected async Task<T?> QuerySingleOrDefaultAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            parameters,
            session.CurrentTransaction,
            cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<T>(command);
    }

    protected async Task<TValue> QuerySingleAsync<TValue>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            parameters,
            session.CurrentTransaction,
            cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<TValue>(command);
    }

    protected async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await session.GetConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            parameters,
            session.CurrentTransaction,
            cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }
}
