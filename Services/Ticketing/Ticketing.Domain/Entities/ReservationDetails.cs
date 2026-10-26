using Ticketing.Domain.Exceptions;

namespace Ticketing.Domain.Entities;

public sealed class ReservationDetails : AuditableEntity
{
    private ReservationDetails()
    {
    }

    public string ContactNameSnapshot { get; private set; } = string.Empty;
    public string ContactEmailSnapshot { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public Reservation Reservation { get; private set; } = null!;

    public ReservationDetails(string contactName, string contactEmail, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(contactName) || string.IsNullOrWhiteSpace(contactEmail))
        {
            throw new DomainException("Reservation contact name and email are required.");
        }

        ContactNameSnapshot = contactName.Trim();
        ContactEmailSnapshot = contactEmail.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
}
