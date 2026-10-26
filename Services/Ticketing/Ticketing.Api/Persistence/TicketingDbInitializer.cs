using Microsoft.EntityFrameworkCore;
using Ticketing.Dal.Persistence;
using Ticketing.Domain.Entities;

namespace Ticketing.Api.Persistence;

public static class TicketingDbInitializer
{
    private static readonly Guid DemoEventPublicId = Guid.Parse("a1000000-0000-4000-8000-000000000001");

    public static async Task SeedAsync(TicketingDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.TicketTypes.AnyAsync(
                ticketType => ticketType.EventPublicId == DemoEventPublicId
                    && ticketType.Name == "Standard",
                cancellationToken))
        {
            return;
        }

        context.TicketTypes.Add(new TicketType(
            DemoEventPublicId,
            "Demo Concert",
            "Standard",
            500m,
            100,
            "UAH",
            "Standard admission to the demo concert."));
        await context.SaveChangesAsync(cancellationToken);
    }
}
