using Ticketing.Bll.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ticketing.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public sealed class TicketsController(ITicketingService ticketingService) : ControllerBase
{
    [HttpPost("{publicId:guid}/use")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Use(Guid publicId, CancellationToken cancellationToken)
    {
        await ticketingService.UseTicketAsync(publicId, cancellationToken);
        return NoContent();
    }
}
