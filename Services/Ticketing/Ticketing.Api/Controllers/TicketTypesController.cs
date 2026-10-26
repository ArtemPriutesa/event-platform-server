using FluentValidation;
using Ticketing.Bll.Contracts;
using Ticketing.Bll.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/ticket-types")]
public sealed class TicketTypesController(ITicketingService ticketingService) : ControllerBase
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

        return Ok(await ticketingService.GetTicketTypesAsync(query, cancellationToken));
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<TicketTypeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketTypeResponse>> GetById(long id, CancellationToken cancellationToken)
        => Ok(await ticketingService.GetTicketTypeAsync(id, cancellationToken));

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
        return NoContent();
    }

    private static Dictionary<string, string[]> ToValidationDictionary(FluentValidation.Results.ValidationResult validation)
        => validation.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
}
