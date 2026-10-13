using EventCatalog.Domain.Repositories;

namespace EventCatalog.Domain.Catalog;

public sealed class Organizer : IEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
