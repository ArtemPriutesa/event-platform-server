namespace EventCatalog.Domain.Events;

public sealed class EventCatalogEvent
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public long OrganizerId { get; set; }
    public string OrganizerName { get; set; } = string.Empty;
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public long Version { get; set; }
    public string? VenueName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public int? Capacity { get; set; }
    public string[] Tags { get; set; } = [];
}
