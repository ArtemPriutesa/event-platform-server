using EventCatalog.Bll.Events;
using EventCatalog.Domain.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace EventCatalog.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class CatalogController(IEventService eventService) : ControllerBase
{
    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<CatalogItem>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CatalogItem>> GetCategories(CancellationToken cancellationToken)
        => eventService.GetCategoriesAsync(cancellationToken);

    [HttpGet("organizers")]
    [ProducesResponseType<IReadOnlyList<CatalogItem>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CatalogItem>> GetOrganizers(CancellationToken cancellationToken)
        => eventService.GetOrganizersAsync(cancellationToken);

    [HttpGet("tags")]
    [ProducesResponseType<IReadOnlyList<CatalogItem>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CatalogItem>> GetTags(CancellationToken cancellationToken)
        => eventService.GetTagsAsync(cancellationToken);
}
