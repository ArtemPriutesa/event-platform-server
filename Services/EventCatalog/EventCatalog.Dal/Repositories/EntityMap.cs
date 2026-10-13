using Npgsql;
using EventCatalog.Domain.Catalog;

namespace EventCatalog.Dal.Repositories;

public sealed record EntityColumn<T>(string Name, Func<T, object?> GetValue);

public sealed class EntityMap<T>(
    string tableName,
    IReadOnlyList<EntityColumn<T>> insertColumns,
    IReadOnlyList<EntityColumn<T>> updateColumns,
    Func<NpgsqlDataReader, T> read)
{
    public string TableName { get; } = tableName;
    public IReadOnlyList<EntityColumn<T>> InsertColumns { get; } = insertColumns;
    public IReadOnlyList<EntityColumn<T>> UpdateColumns { get; } = updateColumns;
    public Func<NpgsqlDataReader, T> Read { get; } = read;
}

public static class CatalogEntityMaps
{
    public static EntityMap<Category> Categories { get; } = new(
        "categories",
        [
            new("name", category => category.Name),
            new("description", category => category.Description)
        ],
        [
            new("name", category => category.Name),
            new("description", category => category.Description)
        ],
        reader => new Category
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Description = reader.IsDBNull(reader.GetOrdinal("description"))
                ? null
                : reader.GetString(reader.GetOrdinal("description"))
        });

    public static EntityMap<Organizer> Organizers { get; } = new(
        "organizers",
        [
            new("name", organizer => organizer.Name),
            new("email", organizer => organizer.Email)
        ],
        [
            new("name", organizer => organizer.Name),
            new("email", organizer => organizer.Email)
        ],
        reader => new Organizer
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Email = reader.GetString(reader.GetOrdinal("email"))
        });

    public static EntityMap<Tag> Tags { get; } = new(
        "tags",
        [new("name", tag => tag.Name)],
        [new("name", tag => tag.Name)],
        reader => new Tag
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name"))
        });
}
