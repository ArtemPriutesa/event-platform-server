using Ticketing.Domain.Exceptions;

namespace Ticketing.Domain.Entities;

public sealed class ReservationItem : AuditableEntity
{
    private ReservationItem()
    {
    }

    public Guid EventPublicId { get; private set; }
    public string EventTitleSnapshot { get; private set; } = string.Empty;
    public string TicketTypeNameSnapshot { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public long ReservationId { get; private set; }
    public Reservation Reservation { get; private set; } = null!;
    public long TicketTypeId { get; private set; }
    public TicketType TicketType { get; private set; } = null!;
    public ICollection<Ticket> Tickets { get; } = new List<Ticket>();

    public ReservationItem(TicketType ticketType, int quantity)
    {
        ArgumentNullException.ThrowIfNull(ticketType);
        if (quantity <= 0)
        {
            throw new DomainException("Reservation item quantity must be positive.");
        }

        EventPublicId = ticketType.EventPublicId;
        EventTitleSnapshot = ticketType.EventTitleSnapshot;
        TicketTypeNameSnapshot = ticketType.Name;
        UnitPrice = ticketType.Price;
        Quantity = quantity;
        TicketType = ticketType;
        TicketTypeId = ticketType.Id;
    }

    public void IssueTickets()
    {
        if (Tickets.Count != 0)
        {
            throw new ConflictException("Tickets have already been issued for this reservation item.");
        }

        for (var i = 0; i < Quantity; i++)
        {
            Tickets.Add(new Ticket(this));
        }
    }
}
