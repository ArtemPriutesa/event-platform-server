using System.Net.Mail;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    public string Value { get; private set; }

    public Email(string value)
    {
        try { Value = new MailAddress(value).Address; }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        { throw new DomainException("Email address is invalid."); }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToUpperInvariant();
    }
}
