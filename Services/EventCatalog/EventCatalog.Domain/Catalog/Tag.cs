using EventCatalog.Domain.Repositories;

namespace EventCatalog.Domain.Catalog;

public sealed class Tag : IEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
