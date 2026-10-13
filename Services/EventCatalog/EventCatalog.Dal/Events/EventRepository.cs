using EventCatalog.Dal.Connection;
using EventCatalog.Dal.Repositories;
using EventCatalog.Domain.Catalog;
using EventCatalog.Domain.Events;
using EventCatalog.Domain.Exceptions;
using Npgsql;

namespace EventCatalog.Dal.Events;

public sealed class EventRepository(IDbSession session)
    : BaseDapperRepository<EventCatalogEvent>(session), IEventRepository
{
    private const string EventSelect = """
        SELECT e.id, e.public_id, e.organizer_id, o.name AS organizer_name,
               e.category_id, c.name AS category_name, e.title, e.description,
               e.starts_at, e.ends_at, e.status, e.created_at, e.updated_at,
               e.is_deleted, e.version, v.venue_name, v.address, v.city, v.country,
               v.capacity, COALESCE(tag_list.names, ARRAY[]::text[]) AS tags
        FROM events e
        JOIN organizers o ON o.id = e.organizer_id AND NOT o.is_deleted
        JOIN categories c ON c.id = e.category_id AND NOT c.is_deleted
        LEFT JOIN event_venues v ON v.event_id = e.id AND NOT v.is_deleted
        LEFT JOIN LATERAL (
            SELECT array_agg(t.name ORDER BY t.name) AS names
            FROM event_tags et
            JOIN tags t ON t.id = et.tag_id AND NOT t.is_deleted
            WHERE et.event_id = e.id AND NOT et.is_deleted
        ) tag_list ON TRUE
        """;

    public async Task<IReadOnlyList<EventCatalogEvent>> GetAllAsync(
        string? search,
        string? status,
        long? categoryId,
        CancellationToken cancellationToken = default)
    {
        var events = await QueryAsync(
            EventSelect + """
            
            WHERE NOT e.is_deleted
              AND (@Search IS NULL OR e.title ILIKE '%' || @Search || '%')
              AND (@Status IS NULL OR e.status = @Status)
              AND (@CategoryId IS NULL OR e.category_id = @CategoryId)
            ORDER BY e.starts_at, e.id;
            """,
            new { Search = NullIfBlank(search), Status = NullIfBlank(status)?.ToUpperInvariant(), CategoryId = categoryId },
            cancellationToken);
        return events;
    }

    public async Task<EventCatalogEvent?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await QuerySingleOrDefaultAsync(
            EventSelect + "\nWHERE e.id = @Id AND NOT e.is_deleted;",
            new { Id = id },
            cancellationToken);
    }

    public async Task<EventCatalogEvent> CreateAsync(
        CreateEventData eventData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await QuerySingleAsync<CreatedEventRow>(
                """
                CALL create_event(
                    @OrganizerId, @CategoryId, @Title, @Description, @StartsAt, @EndsAt,
                    @VenueName, @Address, @City, @Country, @Capacity, NULL::bigint, NULL::uuid
                );
                """,
                eventData,
                cancellationToken);
            return await GetByIdAsync(created.PEventId, cancellationToken)
                ?? throw new EventNotFoundException(created.PEventId);
        }
        catch (PostgresException exception)
        {
            throw Translate(exception);
        }
    }

    public async Task UpdateDraftAsync(
        long id,
        UpdateEventData eventData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteAsync(
                """
                CALL update_event(
                    @Id, @CategoryId, @Title, @Description, @StartsAt, @EndsAt,
                    @VenueName, @Address, @City, @Country, @Capacity
                );
                """,
                new
                {
                    Id = id,
                    eventData.CategoryId,
                    eventData.Title,
                    eventData.Description,
                    eventData.StartsAt,
                    eventData.EndsAt,
                    eventData.VenueName,
                    eventData.Address,
                    eventData.City,
                    eventData.Country,
                    eventData.Capacity
                },
                cancellationToken);
        }
        catch (PostgresException exception)
        {
            throw Translate(exception, id);
        }
    }

    public async Task ChangeStatusAsync(long id, string status, CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteAsync(
                "CALL change_event_status(@Id, @Status);",
                new { Id = id, Status = status.ToUpperInvariant() },
                cancellationToken);
        }
        catch (PostgresException exception)
        {
            throw Translate(exception, id);
        }
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteAsync(
                "CALL soft_delete_event(@Id);",
                new { Id = id },
                cancellationToken);
        }
        catch (PostgresException exception)
        {
            throw Translate(exception, id);
        }
    }

    public async Task AddTagAsync(long eventId, long tagId, CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteAsync(
                "CALL add_event_tag(@EventId, @TagId);",
                new { EventId = eventId, TagId = tagId },
                cancellationToken);
        }
        catch (PostgresException exception)
        {
            throw Translate(exception, eventId);
        }
    }

    public async Task RemoveTagAsync(long eventId, long tagId, CancellationToken cancellationToken = default)
    {
        var affected = await ExecuteAsync(
            """
            UPDATE event_tags
            SET is_deleted = TRUE, updated_at = now(), updated_by = current_user, version = version + 1
            WHERE event_id = @EventId AND tag_id = @TagId AND NOT is_deleted;
            """,
            new { EventId = eventId, TagId = tagId },
            cancellationToken);
        if (affected == 0)
        {
            throw new CatalogReferenceNotFoundException($"Tag {tagId} on event {eventId}");
        }
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Exception Translate(PostgresException exception, long? eventId = null)
        => exception.SqlState switch
        {
            PostgresErrorCodes.NoDataFound => new EventNotFoundException(eventId ?? 0),
            PostgresErrorCodes.ForeignKeyViolation => new CatalogReferenceNotFoundException(exception.MessageText),
            PostgresErrorCodes.InvalidParameterValue => new EventValidationException(exception.MessageText),
            PostgresErrorCodes.CheckViolation or PostgresErrorCodes.UniqueViolation
                => new EventConflictException(exception.MessageText),
            _ => exception
        };

    private sealed class CreatedEventRow
    {
        public long PEventId { get; set; }
        public Guid PPublicId { get; set; }
    }
}
