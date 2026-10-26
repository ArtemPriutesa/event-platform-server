using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Persistence;

public sealed class TicketingDbContext(DbContextOptions<TicketingDbContext> options) : DbContext(options)
{
    public DbSet<TicketType> TicketTypes => Set<TicketType>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationDetails> ReservationDetails => Set<ReservationDetails>();
    public DbSet<ReservationItem> ReservationItems => Set<ReservationItem>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicketingDbContext).Assembly);
}
