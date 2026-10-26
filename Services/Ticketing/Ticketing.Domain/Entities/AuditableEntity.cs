namespace Ticketing.Domain.Entities;

public abstract class AuditableEntity : IEntity
{
    public long Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    public bool IsDeleted { get; private set; }
    public long Version { get; private set; } = 1;

    public void MarkUpdated(DateTimeOffset now)
    {
        UpdatedAt = now;
        Version++;
    }

    public void SoftDelete()
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        MarkUpdated(DateTimeOffset.UtcNow);
    }
}
