using FluentValidation;
using Ticketing.Bll.Contracts;

namespace Ticketing.Bll.Validation;

public sealed class TicketTypeQueryValidator : AbstractValidator<TicketTypeQuery>
{
    public TicketTypeQueryValidator()
    {
        RuleFor(query => query)
            .Must(query => !query.MinPrice.HasValue || !query.MaxPrice.HasValue || query.MinPrice <= query.MaxPrice)
            .WithMessage("MinPrice must be less than or equal to MaxPrice.");
        RuleFor(query => query.SortBy)
            .Must(value => string.Equals(value, "name", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "price", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "event", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortBy must be one of: name, price, event.");
        RuleFor(query => query.SortDir)
            .Must(value => string.Equals(value, "asc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortDir must be 'asc' or 'desc'.");
    }
}

public sealed class CreateTicketTypeRequestValidator : AbstractValidator<CreateTicketTypeRequest>
{
    public CreateTicketTypeRequestValidator()
    {
        RuleFor(request => request.Currency)
            .Must(currency => currency is not null && currency.Length == 3 && currency.All(char.IsAsciiLetter))
            .WithMessage("Currency must be a three-letter ASCII code.");
        RuleFor(request => request.Price)
            .Must(price => decimal.Round(price, 2) == price)
            .WithMessage("Price must have no more than two decimal places.");
    }
}

public sealed class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(request => request.Items)
            .Must(items => items is not null
                && items.Select(item => item.TicketTypeId).Distinct().Count() == items.Count)
            .WithMessage("Each ticket type may appear only once in a reservation.");
    }
}

public sealed class ChangePriceRequestValidator : AbstractValidator<ChangePriceRequest>
{
    public ChangePriceRequestValidator()
    {
        RuleFor(request => request.Price)
            .Must(price => decimal.Round(price, 2) == price)
            .WithMessage("Price must have no more than two decimal places.");
    }
}
