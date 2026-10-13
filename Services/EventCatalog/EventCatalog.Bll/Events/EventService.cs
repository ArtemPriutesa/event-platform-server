using AutoMapper;
using EventCatalog.Dal.Events;
using EventCatalog.Dal.UnitOfWork;
using EventCatalog.Domain.Catalog;
using EventCatalog.Domain.Events;
using EventCatalog.Domain.Exceptions;

namespace EventCatalog.Bll.Events;

public sealed class EventService(IUnitOfWork unitOfWork, IMapper mapper) : IEventService
{
    public async Task<IReadOnlyList<EventResponse>> GetAllAsync(
        string? search,
        string? status,
        long? categoryId,
        CancellationToken cancellationToken)
    {
        ValidateStatusFilter(status);
        if (categoryId is <= 0)
        {
            throw new EventValidationException("Category ID must be greater than zero.");
        }

        var result = await unitOfWork.Events.GetAllAsync(search, status, categoryId, cancellationToken);
        return mapper.Map<IReadOnlyList<EventResponse>>(result);
    }

    public async Task<EventResponse?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        ValidateId(id, nameof(id));
        var item = await unitOfWork.Events.GetByIdAsync(id, cancellationToken);
        return item is null ? null : mapper.Map<EventResponse>(item);
    }

    public Task<EventResponse> CreateAsync(CreateEventRequest request, CancellationToken cancellationToken)
    {
        ValidateId(request.OrganizerId, nameof(request.OrganizerId));
        ValidateId(request.CategoryId, nameof(request.CategoryId));
        ValidateEvent(request.Title, request.StartsAt, request.EndsAt, request.Venue);

        return unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (await unitOfWork.Organizers.GetByIdAsync(request.OrganizerId, token) is null)
            {
                throw new CatalogReferenceNotFoundException($"Organizer {request.OrganizerId}");
            }

            if (await unitOfWork.Categories.GetByIdAsync(request.CategoryId, token) is null)
            {
                throw new CatalogReferenceNotFoundException($"Category {request.CategoryId}");
            }

            var item = await unitOfWork.Events.CreateAsync(new CreateEventData(
                request.OrganizerId,
                request.CategoryId,
                request.Title.Trim(),
                Normalize(request.Description),
                request.StartsAt,
                request.EndsAt,
                request.Venue.Name.Trim(),
                request.Venue.Address.Trim(),
                request.Venue.City.Trim(),
                request.Venue.Country.Trim(),
                request.Venue.Capacity), token);
            return mapper.Map<EventResponse>(item);
        }, cancellationToken);
    }

    public Task<EventResponse> UpdateAsync(long id, UpdateEventRequest request, CancellationToken cancellationToken)
    {
        ValidateId(id, nameof(id));
        ValidateId(request.CategoryId, nameof(request.CategoryId));
        ValidateEvent(request.Title, request.StartsAt, request.EndsAt, request.Venue);

        return unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            if (await unitOfWork.Categories.GetByIdAsync(request.CategoryId, token) is null)
            {
                throw new CatalogReferenceNotFoundException($"Category {request.CategoryId}");
            }

            if (await unitOfWork.Events.GetByIdAsync(id, token) is null)
            {
                throw new EventNotFoundException(id);
            }

            await unitOfWork.Events.UpdateDraftAsync(id, new UpdateEventData(
                request.CategoryId,
                request.Title.Trim(),
                Normalize(request.Description),
                request.StartsAt,
                request.EndsAt,
                request.Venue.Name.Trim(),
                request.Venue.Address.Trim(),
                request.Venue.City.Trim(),
                request.Venue.Country.Trim(),
                request.Venue.Capacity), token);
            var item = await unitOfWork.Events.GetByIdAsync(id, token)
                ?? throw new EventNotFoundException(id);
            return mapper.Map<EventResponse>(item);
        }, cancellationToken);
    }

    public Task PublishAsync(long id, CancellationToken cancellationToken)
        => ChangeStatusAsync(id, "PUBLISHED", cancellationToken);

    public Task FinishAsync(long id, CancellationToken cancellationToken)
        => ChangeStatusAsync(id, "FINISHED", cancellationToken);

    public Task CancelAsync(long id, CancellationToken cancellationToken)
        => ChangeStatusAsync(id, "CANCELLED", cancellationToken);

    public Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        ValidateId(id, nameof(id));
        return unitOfWork.ExecuteInTransactionAsync(
            token => unitOfWork.Events.DeleteAsync(id, token),
            cancellationToken);
    }

    public Task AddTagAsync(long eventId, long tagId, CancellationToken cancellationToken)
    {
        ValidateId(eventId, nameof(eventId));
        ValidateId(tagId, nameof(tagId));
        return unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                if (await unitOfWork.Events.GetByIdAsync(eventId, token) is null)
                {
                    throw new EventNotFoundException(eventId);
                }

                if (await unitOfWork.Tags.GetByIdAsync(tagId, token) is null)
                {
                    throw new CatalogReferenceNotFoundException($"Tag {tagId}");
                }

                await unitOfWork.Events.AddTagAsync(eventId, tagId, token);
            },
            cancellationToken);
    }

    public Task RemoveTagAsync(long eventId, long tagId, CancellationToken cancellationToken)
    {
        ValidateId(eventId, nameof(eventId));
        ValidateId(tagId, nameof(tagId));
        return unitOfWork.ExecuteInTransactionAsync(
            token => unitOfWork.Events.RemoveTagAsync(eventId, tagId, token),
            cancellationToken);
    }

    public async Task<IReadOnlyList<CatalogItem>> GetCategoriesAsync(CancellationToken cancellationToken)
        => mapper.Map<IReadOnlyList<CatalogItem>>(
            await unitOfWork.Categories.GetAllAsync(cancellationToken));

    public async Task<IReadOnlyList<CatalogItem>> GetOrganizersAsync(CancellationToken cancellationToken)
        => mapper.Map<IReadOnlyList<CatalogItem>>(
            await unitOfWork.Organizers.GetAllAsync(cancellationToken));

    public async Task<IReadOnlyList<CatalogItem>> GetTagsAsync(CancellationToken cancellationToken)
        => mapper.Map<IReadOnlyList<CatalogItem>>(
            await unitOfWork.Tags.GetAllAsync(cancellationToken));

    private Task ChangeStatusAsync(long id, string status, CancellationToken cancellationToken)
    {
        ValidateId(id, nameof(id));
        return unitOfWork.ExecuteInTransactionAsync(
            token => unitOfWork.Events.ChangeStatusAsync(id, status, token),
            cancellationToken);
    }

    private static void ValidateEvent(string title, DateTimeOffset startsAt, DateTimeOffset endsAt, VenueRequest venue)
    {
        if (venue is null)
        {
            throw new EventValidationException("Venue details are required.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new EventValidationException("Event title is required.");
        }

        if (string.IsNullOrWhiteSpace(venue.Name)
            || string.IsNullOrWhiteSpace(venue.Address)
            || string.IsNullOrWhiteSpace(venue.City)
            || string.IsNullOrWhiteSpace(venue.Country))
        {
            throw new EventValidationException("Venue name, address, city, and country are required.");
        }

        if (endsAt <= startsAt)
        {
            throw new EventValidationException("Event end time must be later than its start time.");
        }

        if (venue.Capacity <= 0)
        {
            throw new EventValidationException("Venue capacity must be greater than zero.");
        }
    }

    private static void ValidateId(long id, string name)
    {
        if (id <= 0)
        {
            throw new EventValidationException($"{name} must be greater than zero.");
        }
    }

    private static void ValidateStatusFilter(string? status)
    {
        if (status is not null
            && !new[] { "DRAFT", "PUBLISHED", "FINISHED", "CANCELLED" }
                .Contains(status.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new EventValidationException("Status must be DRAFT, PUBLISHED, FINISHED, or CANCELLED.");
        }
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
