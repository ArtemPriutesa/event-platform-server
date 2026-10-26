using Ticketing.Domain.Exceptions;

namespace Ticketing.Domain.Entities;

public sealed class Reservation : AuditableEntity
{
    private Reservation()
    {
    }

    public Guid PublicId { get; private set; }
    public Guid UserPublicId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public ReservationDetails Details { get; private set; } = null!;
    public ICollection<ReservationItem> Items { get; } = new List<ReservationItem>();

    public Reservation(Guid userPublicId, DateTimeOffset expiresAt)
    {
        if (userPublicId == Guid.Empty)
        {
            throw new DomainException("User ID is required.");
        }

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            throw new DomainException("Reservation expiry must be in the future.");
        }

        PublicId = Guid.NewGuid();
        UserPublicId = userPublicId;
        Status = ReservationStatus.Held;
        ExpiresAt = expiresAt;
    }

    public void AddDetails(ReservationDetails details)
    {
        if (Details is not null)
        {
            throw new DomainException("Reservation details have already been assigned.");
        }

        Details = details;
    }

    public void AddItem(ReservationItem item)
    {
        if (Status != ReservationStatus.Held)
        {
            throw new ConflictException("Items can only be added to a held reservation.");
        }

        if (Items.Any(existing => existing.TicketTypeId == item.TicketTypeId))
        {
            throw new ConflictException("A reservation cannot contain duplicate ticket types.");
        }

        Items.Add(item);
    }

    public void Confirm()
    {
        EnsureHeld();
        if (ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new ConflictException("The reservation has expired.");
        }

        Status = ReservationStatus.Confirmed;
    }

    public void Cancel()
    {
        EnsureHeld();
        Status = ReservationStatus.Cancelled;
    }

    public void Expire()
    {
        EnsureHeld();
        Status = ReservationStatus.Expired;
    }

    private void EnsureHeld()
    {
        if (Status != ReservationStatus.Held)
        {
            throw new ConflictException($"Reservation cannot transition from {Status}.");
        }
    }
}
