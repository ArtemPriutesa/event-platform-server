using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ticketing.Dal.Persistence;

public sealed class TicketingDbContextFactory : IDesignTimeDbContextFactory<TicketingDbContext>
{
    public TicketingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Ticketing")
            ?? "Host=localhost;Database=ticketing;Username=postgres";
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new TicketingDbContext(options);
    }
}
