using FluentValidation;
using Ticketing.Bll.Contracts;
using Ticketing.Bll.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/reservations")]
public sealed class ReservationsController(ITicketingService ticketingService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> Create(
        CreateReservationRequest request,
        [FromServices] IValidator<CreateReservationRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        var created = await ticketingService.CreateReservationAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> GetById(long id, CancellationToken cancellationToken)
        => Ok(await ticketingService.GetReservationAsync(id, cancellationToken));

    [HttpGet("{id:long}/explicit")]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> GetExplicit(long id, CancellationToken cancellationToken)
        => Ok(await ticketingService.GetReservationExplicitAsync(id, cancellationToken));

    [HttpGet("{id:long}/ticket-types")]
    [ProducesResponseType<IReadOnlyList<TicketTypeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TicketTypeResponse>>> GetTicketTypes(
        long id,
        CancellationToken cancellationToken)
        => Ok(await ticketingService.GetReservationTicketTypesAsync(id, cancellationToken));

    [HttpPost("{id:long}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(long id, CancellationToken cancellationToken)
    {
        await ticketingService.ConfirmReservationAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(long id, CancellationToken cancellationToken)
    {
        await ticketingService.CancelReservationAsync(id, cancellationToken);
        return NoContent();
    }
}
