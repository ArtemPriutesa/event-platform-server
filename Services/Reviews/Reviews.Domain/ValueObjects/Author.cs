using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

public sealed class Author : ValueObject
{
    public string DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public Email? Email { get; private set; }

    public Author(string displayName, string? avatarUrl = null, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Author display name is required.");
        if (displayName.Trim().Length > 120)
            throw new DomainException("Author display name cannot exceed 120 characters.");
        if (avatarUrl is not null && !Uri.TryCreate(avatarUrl, UriKind.Absolute, out _))
            throw new DomainException("Author avatar URL must be an absolute URL.");

        DisplayName = displayName.Trim();
        AvatarUrl = avatarUrl;
        Email = string.IsNullOrWhiteSpace(email) ? null : new Email(email);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DisplayName;
        yield return AvatarUrl;
        yield return Email;
    }
}
