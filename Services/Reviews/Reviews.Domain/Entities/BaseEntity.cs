using MongoDB.Bson;

namespace Reviews.Domain.Entities;

public abstract class BaseEntity
{
    public ObjectId Id { get; private set; } = ObjectId.GenerateNewId();
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public long Version { get; private set; } = 1;

    protected void MarkUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }
}
