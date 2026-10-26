using Ticketing.Bll.Contracts;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Specifications;

namespace Ticketing.Bll.Specifications;

public sealed class TicketTypeFilterSpecification : Specification<TicketType>
{
    public TicketTypeFilterSpecification(TicketTypeQuery query)
    {
        Criteria.Add(ticketType => !ticketType.IsDeleted);

        if (query.EventPublicId.HasValue)
        {
            var eventPublicId = query.EventPublicId.Value;
            Criteria.Add(ticketType => ticketType.EventPublicId == eventPublicId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            var normalizedSearch = search.ToLowerInvariant();
            Criteria.Add(ticketType => ticketType.Name.ToLower().Contains(normalizedSearch)
                || ticketType.EventTitleSnapshot.ToLower().Contains(normalizedSearch));
        }

        if (query.MinPrice.HasValue)
        {
            var minPrice = query.MinPrice.Value;
            Criteria.Add(ticketType => ticketType.Price >= minPrice);
        }

        if (query.MaxPrice.HasValue)
        {
            var maxPrice = query.MaxPrice.Value;
            Criteria.Add(ticketType => ticketType.Price <= maxPrice);
        }

        var descending = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        switch (query.SortBy.ToLowerInvariant())
        {
            case "price":
                if (descending) OrderByDescending = ticketType => ticketType.Price;
                else OrderBy = ticketType => ticketType.Price;
                break;
            case "event":
                if (descending) OrderByDescending = ticketType => ticketType.EventTitleSnapshot;
                else OrderBy = ticketType => ticketType.EventTitleSnapshot;
                break;
            default:
                if (descending) OrderByDescending = ticketType => ticketType.Name;
                else OrderBy = ticketType => ticketType.Name;
                break;
        }

        Skip = (query.Page - 1) * query.PageSize;
        Take = query.PageSize;
    }
}
