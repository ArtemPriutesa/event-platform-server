using System.ComponentModel.DataAnnotations;
using Ticketing.Domain.Entities;

namespace Ticketing.Bll.Contracts;

public sealed record TicketTypeResponse(
    long Id,
    Guid EventPublicId,
    string EventTitleSnapshot,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    int TotalQuantity,
    int ReservedQuantity,
    int SoldQuantity,
    int AvailableQuantity);

public sealed record TicketTypeQuery(
    [property: Range(1, int.MaxValue)] int Page = 1,
    [property: Range(1, 100)] int PageSize = 20,
    Guid? EventPublicId = null,
    string? Search = null,
    [property: Range(typeof(decimal), "0", "9999999999999999.99")] decimal? MinPrice = null,
    [property: Range(typeof(decimal), "0", "9999999999999999.99")] decimal? MaxPrice = null,
    string SortBy = "name",
    string SortDir = "asc");

public sealed class CreateTicketTypeRequest
{
    [Required]
    public Guid EventPublicId { get; init; }

    [Required, StringLength(250, MinimumLength = 1)]
    public string EventTitleSnapshot { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Price { get; init; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = "UAH";

    [Range(0, int.MaxValue)]
    public int TotalQuantity { get; init; }
}

public sealed class ChangePriceRequest
{
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Price { get; init; }
}

public sealed record ReservationResponse(
    long Id,
    Guid PublicId,
    Guid UserPublicId,
    ReservationStatus Status,
    DateTimeOffset ExpiresAt,
    ReservationDetailsResponse? Details,
    IReadOnlyList<ReservationItemResponse> Items);

public sealed record ReservationDetailsResponse(
    string ContactNameSnapshot,
    string ContactEmailSnapshot,
    string? Notes);

public sealed record ReservationItemResponse(
    long TicketTypeId,
    string EventTitleSnapshot,
    string TicketTypeNameSnapshot,
    decimal UnitPrice,
    int Quantity,
    IReadOnlyList<TicketResponse> Tickets);

public sealed record TicketResponse(Guid PublicId, TicketStatus Status, DateTimeOffset IssuedAt, DateTimeOffset? UsedAt);

public sealed class CreateReservationRequest
{
    [Required]
    public Guid UserPublicId { get; init; }

    [Required]
    public ReservationContactRequest Contact { get; init; } = new();

    [Required, MinLength(1)]
    public List<ReservationItemRequest> Items { get; init; } = [];
}

public sealed class ReservationContactRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(320)]
    public string Email { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; init; }
}

public sealed class ReservationItemRequest
{
    [Range(1, long.MaxValue)]
    public long TicketTypeId { get; init; }

    [Range(1, 100)]
    public int Quantity { get; init; }
}
