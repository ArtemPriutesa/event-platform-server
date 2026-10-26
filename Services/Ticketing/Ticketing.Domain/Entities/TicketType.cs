using Ticketing.Domain.Exceptions;

namespace Ticketing.Domain.Entities;

public sealed class TicketType : AuditableEntity
{
    private TicketType()
    {
    }

    public Guid EventPublicId { get; private set; }
    public string EventTitleSnapshot { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "UAH";
    public int TotalQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int SoldQuantity { get; private set; }
    public int AvailableQuantity => TotalQuantity - ReservedQuantity - SoldQuantity;
    public ICollection<ReservationItem> ReservationItems { get; } = new List<ReservationItem>();

    public TicketType(
        Guid eventPublicId,
        string eventTitleSnapshot,
        string name,
        decimal price,
        int totalQuantity,
        string currency = "UAH",
        string? description = null)
    {
        if (eventPublicId == Guid.Empty)
        {
            throw new DomainException("Event ID is required.");
        }

        if (string.IsNullOrWhiteSpace(eventTitleSnapshot) || string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Event title and ticket type name are required.");
        }

        if (price < 0)
        {
            throw new DomainException("Ticket price cannot be negative.");
        }

        if (totalQuantity < 0)
        {
            throw new DomainException("Ticket quantity cannot be negative.");
        }

        EventPublicId = eventPublicId;
        EventTitleSnapshot = eventTitleSnapshot.Trim();
        Name = name.Trim();
        Price = price;
        TotalQuantity = totalQuantity;
        Currency = NormalizeCurrency(currency);
        Description = Normalize(description);
    }

    public void ChangePrice(decimal price)
    {
        if (price < 0)
        {
            throw new DomainException("Ticket price cannot be negative.");
        }

        Price = price;
    }

    public void ChangeQuantity(int totalQuantity)
    {
        if (totalQuantity < ReservedQuantity + SoldQuantity)
        {
            throw new DomainException("Total quantity cannot be less than reserved and sold tickets.");
        }

        TotalQuantity = totalQuantity;
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Reservation quantity must be positive.");
        }

        if (AvailableQuantity < quantity)
        {
            throw new ConflictException("Not enough tickets are available.");
        }

        ReservedQuantity += quantity;
    }

    public void ReleaseReservation(int quantity)
    {
        if (quantity <= 0 || ReservedQuantity < quantity)
        {
            throw new DomainException("Cannot release the requested reservation quantity.");
        }

        ReservedQuantity -= quantity;
    }

    public void SellReserved(int quantity)
    {
        if (quantity <= 0 || ReservedQuantity < quantity)
        {
            throw new DomainException("Cannot sell the requested reserved quantity.");
        }

        ReservedQuantity -= quantity;
        SoldQuantity += quantity;
    }

    private static string NormalizeCurrency(string currency)
    {
        var normalized = currency.Trim().ToUpperInvariant();
        if (normalized.Length != 3)
        {
            throw new DomainException("Currency must be a three-letter code.");
        }

        return normalized;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
