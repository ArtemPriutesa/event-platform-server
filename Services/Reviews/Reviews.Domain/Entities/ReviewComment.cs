using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

public sealed class ReviewComment
{
    public Author Author { get; private set; }
    public string Text { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ReviewComment() { Author = null!; Text = string.Empty; }

    public ReviewComment(Author author, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new DomainException("Comment text is required.");
        if (text.Trim().Length > 2000) throw new DomainException("Comment cannot exceed 2000 characters.");
        Author = author;
        Text = text.Trim();
        CreatedAt = DateTime.UtcNow;
    }
}
