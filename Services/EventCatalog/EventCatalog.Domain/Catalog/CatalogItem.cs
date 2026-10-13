namespace EventCatalog.Domain.Catalog;

public sealed class CatalogItem
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
