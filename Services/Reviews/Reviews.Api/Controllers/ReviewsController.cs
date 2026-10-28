using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reviews.Application.Abstractions;
using Reviews.Application.Reviews;

namespace Reviews.Api.Controllers;

[ApiController]
[Route("api/reviews")]
public sealed class ReviewsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ReviewDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReviewDto>>> GetList(
        [FromQuery] string? eventPublicId, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetReviewsListQuery(eventPublicId, search, page, pageSize), cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<ReviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDto>> GetById(string id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetReviewByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType<ReviewDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDto>> Create(CreateReviewCommand command, CancellationToken cancellationToken)
    {
        var created = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(string id, UpdateReviewBody body, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateReviewCommand(id, body.ExpectedVersion, body.Rating, body.Text, body.Photos), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string id, [FromQuery] long expectedVersion, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteReviewCommand(id, expectedVersion), cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateReviewBody(long ExpectedVersion, int Rating, string Text, IReadOnlyList<string>? Photos);
