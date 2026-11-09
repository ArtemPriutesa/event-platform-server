using EventCatalog.Bll.Events;
using Microsoft.AspNetCore.Mvc;
using Platform.ServiceDefaults.Caching;

namespace EventCatalog.Api.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController(IEventService eventService, IHybridCache cache) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EventResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EventResponse>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] long? categoryId,
        CancellationToken cancellationToken)
    {
        var events = string.IsNullOrWhiteSpace(search) && string.IsNullOrWhiteSpace(status) && categoryId is null
            ? await cache.GetOrCreateAsync<IReadOnlyList<EventResponse>>("catalog:events:list",
                token => eventService.GetAllAsync(null, null, null, token), cancellationToken: cancellationToken)
            : await eventService.GetAllAsync(search, status, categoryId, cancellationToken);
        return Ok(events);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetById(long id, CancellationToken cancellationToken)
    {
        var item = await cache.GetOrCreateAsync<EventResponse?>($"catalog:event:{id}",
            token => eventService.GetByIdAsync(id, token), cancellationToken: cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [ProducesResponseType<EventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventResponse>> Create(
        CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var created = await eventService.CreateAsync(request, cancellationToken);
        await cache.RemoveAsync("catalog:events:list", "event created", cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        long id,
        UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        await eventService.UpdateAsync(id, request, cancellationToken);
        await InvalidateEventAsync(id, "event updated", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Publish(long id, CancellationToken cancellationToken)
    {
        await eventService.PublishAsync(id, cancellationToken);
        await InvalidateEventAsync(id, "event published", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/finish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Finish(long id, CancellationToken cancellationToken)
    {
        await eventService.FinishAsync(id, cancellationToken);
        await InvalidateEventAsync(id, "event finished", cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(long id, CancellationToken cancellationToken)
    {
        await eventService.CancelAsync(id, cancellationToken);
        await InvalidateEventAsync(id, "event cancelled", cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await eventService.DeleteAsync(id, cancellationToken);
        await InvalidateEventAsync(id, "event deleted", cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:long}/tags/{tagId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddTag(long id, long tagId, CancellationToken cancellationToken)
    {
        await eventService.AddTagAsync(id, tagId, cancellationToken);
        await InvalidateEventAsync(id, "event tag added", cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:long}/tags/{tagId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveTag(long id, long tagId, CancellationToken cancellationToken)
    {
        await eventService.RemoveTagAsync(id, tagId, cancellationToken);
        await InvalidateEventAsync(id, "event tag removed", cancellationToken);
        return NoContent();
    }

    private async Task InvalidateEventAsync(long id, string reason, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync($"catalog:event:{id}", reason, cancellationToken);
        await cache.RemoveAsync("catalog:events:list", reason, cancellationToken);
    }
}
