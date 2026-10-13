using System.ComponentModel.DataAnnotations;
using EventCatalog.Domain.Catalog;
using EventCatalog.Domain.Events;

namespace EventCatalog.Bll.Events;

public sealed class EventResponse
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public long OrganizerId { get; init; }
    public string OrganizerName { get; init; } = string.Empty;
    public long CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTimeOffset StartsAt { get; init; }
    public DateTimeOffset EndsAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public long Version { get; init; }
    public VenueResponse? Venue { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}

public sealed record VenueResponse(string Name, string Address, string City, string Country, int Capacity);

public sealed class CreateEventRequest
{
    [Range(1, long.MaxValue)]
    public long OrganizerId { get; init; }

    [Range(1, long.MaxValue)]
    public long CategoryId { get; init; }

    [Required, StringLength(250, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }
    public DateTimeOffset StartsAt { get; init; }
    public DateTimeOffset EndsAt { get; init; }
    [Required]
    public VenueRequest Venue { get; init; } = new();
}

public sealed class UpdateEventRequest
{
    [Range(1, long.MaxValue)]
    public long CategoryId { get; init; }

    [Required, StringLength(250, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }
    public DateTimeOffset StartsAt { get; init; }
    public DateTimeOffset EndsAt { get; init; }
    [Required]
    public VenueRequest Venue { get; init; } = new();
}

public sealed class VenueRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(400, MinimumLength = 1)]
    public string Address { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string City { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Country { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Capacity { get; init; }
}

public sealed class EventCatalogMappingProfile : AutoMapper.Profile
{
    public EventCatalogMappingProfile()
    {
        CreateMap<EventCatalogEvent, EventResponse>()
            .MaxDepth(16)
            .ForMember(
                destination => destination.Venue,
                options => options.MapFrom(source => source.VenueName == null
                    ? null
                    : new VenueResponse(
                        source.VenueName,
                        source.Address!,
                        source.City!,
                        source.Country!,
                        source.Capacity!.Value)));
        CreateMap<Category, CatalogItem>().MaxDepth(16);
        CreateMap<Organizer, CatalogItem>()
            .MaxDepth(16)
            .ForMember(destination => destination.Description, options => options.MapFrom(_ => (string?)null));
        CreateMap<Tag, CatalogItem>()
            .MaxDepth(16)
            .ForMember(destination => destination.Description, options => options.MapFrom(_ => (string?)null));
    }
}

public interface IEventService
{
    Task<IReadOnlyList<EventResponse>> GetAllAsync(string? search, string? status, long? categoryId, CancellationToken cancellationToken);
    Task<EventResponse?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<EventResponse> CreateAsync(CreateEventRequest request, CancellationToken cancellationToken);
    Task<EventResponse> UpdateAsync(long id, UpdateEventRequest request, CancellationToken cancellationToken);
    Task PublishAsync(long id, CancellationToken cancellationToken);
    Task FinishAsync(long id, CancellationToken cancellationToken);
    Task CancelAsync(long id, CancellationToken cancellationToken);
    Task DeleteAsync(long id, CancellationToken cancellationToken);
    Task AddTagAsync(long eventId, long tagId, CancellationToken cancellationToken);
    Task RemoveTagAsync(long eventId, long tagId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CatalogItem>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<CatalogItem>> GetOrganizersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<CatalogItem>> GetTagsAsync(CancellationToken cancellationToken);
}
