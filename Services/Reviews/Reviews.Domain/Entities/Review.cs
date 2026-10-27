using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

public sealed class Review : BaseEntity
{
    private readonly List<ReviewComment> _comments = [];
    private readonly List<string> _photos = [];

    public string EventPublicId { get; private set; } = string.Empty;
    public string EventTitleSnapshot { get; private set; } = string.Empty;
    public string TicketPublicId { get; private set; } = string.Empty;
    public string UserPublicId { get; private set; } = string.Empty;
    public Author Author { get; private set; } = null!;
    public int Rating { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public IReadOnlyCollection<string> Photos => _photos.AsReadOnly();
    public IReadOnlyCollection<ReviewComment> Comments => _comments.AsReadOnly();
    public string? DiscussionId { get; private set; }

    private Review() { }

    public Review(string eventPublicId, string eventTitleSnapshot, string ticketPublicId,
        string userPublicId, Author author, int rating, string text,
        IEnumerable<string>? photos = null, string? discussionId = null)
    {
        EventPublicId = Required(eventPublicId, nameof(eventPublicId));
        EventTitleSnapshot = Required(eventTitleSnapshot, nameof(eventTitleSnapshot));
        TicketPublicId = Required(ticketPublicId, nameof(ticketPublicId));
        UserPublicId = Required(userPublicId, nameof(userPublicId));
        Author = author ?? throw new DomainException("Review author is required.");
        ChangeRating(rating);
        ChangeText(text);
        if (photos is not null) _photos.AddRange(photos.Select(ValidatePhoto));
        DiscussionId = string.IsNullOrWhiteSpace(discussionId) ? null : discussionId.Trim();
    }

    public void Update(int rating, string text, IEnumerable<string>? photos)
    {
        ChangeRating(rating);
        ChangeText(text);
        _photos.Clear();
        if (photos is not null) _photos.AddRange(photos.Select(ValidatePhoto));
        MarkUpdated();
    }

    public void AddComment(Author author, string text)
    {
        _comments.Add(new ReviewComment(author, text));
        MarkUpdated();
    }

    private void ChangeRating(int rating)
    {
        if (rating is < 1 or > 5) throw new DomainException("Rating must be between 1 and 5.");
        Rating = rating;
    }

    private void ChangeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new DomainException("Review text is required.");
        if (text.Trim().Length > 5000) throw new DomainException("Review text cannot exceed 5000 characters.");
        Text = text.Trim();
    }

    private static string Required(string value, string field)
        => string.IsNullOrWhiteSpace(value) ? throw new DomainException($"{field} is required.") : value.Trim();

    private static string ValidatePhoto(string photo)
        => Uri.TryCreate(photo, UriKind.Absolute, out _)
            ? photo
            : throw new DomainException("Review photo URLs must be absolute URLs.");
}
