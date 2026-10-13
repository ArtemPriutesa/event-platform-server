using System.Data;
using EventCatalog.Dal.Connection;
using EventCatalog.Domain.Repositories;
using Npgsql;

namespace EventCatalog.Dal.Repositories;

public class AdoNetRepository<T> : IGenericRepository<T>
    where T : class, IEntity
{
    protected IDbSession Session { get; }
    protected EntityMap<T> Map { get; }

    public AdoNetRepository(IDbSession session, EntityMap<T> map)
    {
        Session = session;
        Map = map;
    }

    public async Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var connection = await Session.GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = Session.CurrentTransaction;
        command.CommandText = $"SELECT * FROM {Map.TableName} WHERE id = @id AND NOT is_deleted;";
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map.Read(reader) : null;
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var connection = await Session.GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = Session.CurrentTransaction;
        command.CommandText = $"SELECT * FROM {Map.TableName} WHERE NOT is_deleted ORDER BY id;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(Map.Read(reader));
        }

        return result;
    }

    public async Task<long> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var columns = string.Join(", ", Map.InsertColumns.Select(column => column.Name));
        var parameters = string.Join(", ", Map.InsertColumns.Select((_, index) => $"@value{index}"));
        var connection = await Session.GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = Session.CurrentTransaction;
        command.CommandText = $"""
            INSERT INTO {Map.TableName} ({columns}, created_by, updated_by)
            VALUES ({parameters}, current_user, current_user)
            RETURNING id;
            """;

        AddValues(command, entity, Map.InsertColumns);
        var id = await command.ExecuteScalarAsync(cancellationToken);
        return id is long value
            ? value
            : throw new DataException($"Insert into {Map.TableName} did not return an identity.");
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var setClause = string.Join(", ", Map.UpdateColumns.Select((column, index) => $"{column.Name} = @value{index}"));
        var connection = await Session.GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = Session.CurrentTransaction;
        command.CommandText = $"""
            UPDATE {Map.TableName}
            SET {setClause}, updated_at = now(), updated_by = current_user, version = version + 1
            WHERE id = @id AND NOT is_deleted;
            """;
        AddValues(command, entity, Map.UpdateColumns);
        command.Parameters.AddWithValue("id", entity.Id);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new KeyNotFoundException($"{typeof(T).Name} with ID {entity.Id} was not found.");
        }
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var connection = await Session.GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = Session.CurrentTransaction;
        command.CommandText = $"""
            UPDATE {Map.TableName}
            SET is_deleted = TRUE, updated_at = now(), updated_by = current_user, version = version + 1
            WHERE id = @id AND NOT is_deleted;
            """;
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void AddValues(
        NpgsqlCommand command,
        T entity,
        IReadOnlyList<EntityColumn<T>> columns)
    {
        for (var index = 0; index < columns.Count; index++)
        {
            command.Parameters.AddWithValue($"value{index}", columns[index].GetValue(entity) ?? DBNull.Value);
        }
    }

    protected async Task<T?> GetByValueAsync(
        string columnName,
        object value,
        CancellationToken cancellationToken = default)
    {
        var connection = await Session.GetConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = Session.CurrentTransaction;
        command.CommandText = $"SELECT * FROM {Map.TableName} WHERE {columnName} = @value AND NOT is_deleted;";
        command.Parameters.AddWithValue("value", value);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map.Read(reader) : null;
    }
}
