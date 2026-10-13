using EventCatalog.Domain.Catalog;
using EventCatalog.Dal.Connection;

namespace EventCatalog.Dal.Repositories;

public sealed class AdoNetCategoryRepository(IDbSession session)
    : AdoNetRepository<Category>(session, CatalogEntityMaps.Categories), ICategoryRepository
{
    public Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => GetByValueAsync("name", name, cancellationToken);
}

public sealed class AdoNetOrganizerRepository(IDbSession session)
    : AdoNetRepository<Organizer>(session, CatalogEntityMaps.Organizers), IOrganizerRepository
{
    public Task<Organizer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => GetByValueAsync("email", email, cancellationToken);
}

public sealed class AdoNetTagRepository(IDbSession session)
    : AdoNetRepository<Tag>(session, CatalogEntityMaps.Tags), ITagRepository
{
    public Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => GetByValueAsync("name", name, cancellationToken);
}
