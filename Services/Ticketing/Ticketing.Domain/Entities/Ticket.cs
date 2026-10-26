using Ticketing.Domain.Exceptions;

namespace Ticketing.Domain.Entities;

public sealed class Ticket : AuditableEntity
{
    private Ticket()
    {
    }

    public Guid PublicId { get; private set; }
    public long ReservationItemId { get; private set; }
    public long TicketTypeId { get; private set; }
    public TicketStatus Status { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public ReservationItem ReservationItem { get; private set; } = null!;

    internal Ticket(ReservationItem item)
    {
        ReservationItem = item;
        ReservationItemId = item.Id;
        TicketTypeId = item.TicketTypeId;
        PublicId = Guid.NewGuid();
        Status = TicketStatus.Valid;
        IssuedAt = DateTimeOffset.UtcNow;
    }

    public void MarkUsed()
    {
        if (Status != TicketStatus.Valid)
        {
            throw new ConflictException($"Ticket cannot transition from {Status} to Used.");
        }

        Status = TicketStatus.Used;
        UsedAt = DateTimeOffset.UtcNow;
    }
}
