using FluentValidation;
using Ticketing.Bll.Contracts;
using Ticketing.Bll.Services;
using Microsoft.AspNetCore.Mvc;
using Platform.ServiceDefaults.Caching;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/ticket-types")]
public sealed class TicketTypesController(ITicketingService ticketingService, IHybridCache cache) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<TicketTypeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<TicketTypeResponse>>> GetList(
        [FromQuery] TicketTypeQuery query,
        [FromServices] IValidator<TicketTypeQuery> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(ToValidationDictionary(validation)));
        }

        var isDefaultQuery = query.Page == 1 && query.PageSize == 20 && query.EventPublicId is null
            && string.IsNullOrWhiteSpace(query.Search) && query.MinPrice is null && query.MaxPrice is null
            && string.Equals(query.SortBy, "name", StringComparison.OrdinalIgnoreCase)
            && string.Equals(query.SortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var result = isDefaultQuery
            ? await cache.GetOrCreateAsync("catalog:products:list",
                token => ticketingService.GetTicketTypesAsync(query, token), cancellationToken: cancellationToken)
            : await ticketingService.GetTicketTypesAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<TicketTypeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketTypeResponse>> GetById(long id, CancellationToken cancellationToken)
        => Ok(await cache.GetOrCreateAsync($"catalog:product:{id}",
            token => ticketingService.GetTicketTypeAsync(id, token), cancellationToken: cancellationToken));

    [HttpPost]
    [ProducesResponseType<TicketTypeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketTypeResponse>> Create(
        CreateTicketTypeRequest request,
        [FromServices] IValidator<CreateTicketTypeRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(ToValidationDictionary(validation)));
        }

        var created = await ticketingService.CreateTicketTypeAsync(request, cancellationToken);
        await cache.RemoveAsync("catalog:products:list", "ticket type created", cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}/price")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePrice(
        long id,
        ChangePriceRequest request,
        [FromServices] IValidator<ChangePriceRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(ToValidationDictionary(validation)));
        }

        await ticketingService.ChangeTicketTypePriceAsync(id, request.Price, cancellationToken);
        await cache.RemoveAsync($"catalog:product:{id}", "ticket type price changed", cancellationToken);
        await cache.RemoveAsync("catalog:products:list", "ticket type price changed", cancellationToken);
        return NoContent();
    }

    private static Dictionary<string, string[]> ToValidationDictionary(FluentValidation.Results.ValidationResult validation)
        => validation.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
}
