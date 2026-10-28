using FluentValidation;
using MongoDB.Bson;
using Reviews.Application.Abstractions;
using Reviews.Application.Reviews;

namespace Reviews.Application.Validation;

public sealed class CreateReviewValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewValidator(IReviewQueries queries)
    {
        RuleFor(x => x.EventPublicId).NotEmpty();
        RuleFor(x => x.EventTitleSnapshot).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TicketPublicId).NotEmpty();
        RuleFor(x => x.UserPublicId).NotEmpty();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.AvatarUrl).Must(BeAbsoluteUrl).When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl));
        RuleFor(x => x.AuthorEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.AuthorEmail));
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Photos).Must(photos => photos is null || photos.Count <= 10);
        RuleFor(x => x).MustAsync(async (command, token) =>
                !await queries.ExistsForUserAndEventAsync(command.UserPublicId, command.EventPublicId, token))
            .WithMessage("This user has already reviewed the event.");
    }

    private static bool BeAbsoluteUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out _);
}

public sealed class UpdateReviewValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewValidator()
    {
        RuleFor(x => x.Id).Must(id => ObjectId.TryParse(id, out _)).WithMessage("Id must be a valid MongoDB ObjectId.");
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Photos).Must(photos => photos is null || photos.Count <= 10);
    }
}

public sealed class DeleteReviewValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewValidator()
    {
        RuleFor(x => x.Id).Must(id => ObjectId.TryParse(id, out _)).WithMessage("Id must be a valid MongoDB ObjectId.");
        RuleFor(x => x.ExpectedVersion).GreaterThan(0);
    }
}

public sealed class GetReviewByIdValidator : AbstractValidator<GetReviewByIdQuery>
{
    public GetReviewByIdValidator()
        => RuleFor(x => x.Id).Must(id => ObjectId.TryParse(id, out _)).WithMessage("Id must be a valid MongoDB ObjectId.");
}

public sealed class GetReviewsListValidator : AbstractValidator<GetReviewsListQuery>
{
    public GetReviewsListValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
