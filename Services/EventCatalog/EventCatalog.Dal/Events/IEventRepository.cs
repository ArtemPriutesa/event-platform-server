using EventCatalog.Domain.Events;

namespace EventCatalog.Dal.Events;

public interface IEventRepository
{
    Task<IReadOnlyList<EventCatalogEvent>> GetAllAsync(
        string? search,
        string? status,
        long? categoryId,
        CancellationToken cancellationToken = default);

    Task<EventCatalogEvent?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<EventCatalogEvent> CreateAsync(CreateEventData eventData, CancellationToken cancellationToken = default);
    Task UpdateDraftAsync(long id, UpdateEventData eventData, CancellationToken cancellationToken = default);
    Task ChangeStatusAsync(long id, string status, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task AddTagAsync(long eventId, long tagId, CancellationToken cancellationToken = default);
    Task RemoveTagAsync(long eventId, long tagId, CancellationToken cancellationToken = default);
}

public sealed record CreateEventData(
    long OrganizerId,
    long CategoryId,
    string Title,
    string? Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string VenueName,
    string Address,
    string City,
    string Country,
    int Capacity);

public sealed record UpdateEventData(
    long CategoryId,
    string Title,
    string? Description,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string VenueName,
    string Address,
    string City,
    string Country,
    int Capacity);
