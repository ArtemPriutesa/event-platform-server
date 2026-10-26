using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Persistence.Configurations;

public sealed class ReservationItemConfiguration : IEntityTypeConfiguration<ReservationItem>
{
    public void Configure(EntityTypeBuilder<ReservationItem> builder)
    {
        builder.ToTable("reservation_items", table =>
        {
            table.HasCheckConstraint("reservation_items_unit_price_check", "unit_price >= 0");
            table.HasCheckConstraint("reservation_items_quantity_check", "quantity > 0");
            table.HasCheckConstraint("reservation_items_version_check", "version > 0");
        });
        AuditableEntityConfiguration.Configure(builder);
        builder.Property(item => item.ReservationId).HasColumnName("reservation_id");
        builder.Property(item => item.TicketTypeId).HasColumnName("ticket_type_id");
        builder.Property(item => item.EventPublicId).HasColumnName("event_public_id").IsRequired();
        builder.Property(item => item.EventTitleSnapshot).HasColumnName("event_title_snapshot").HasMaxLength(250).IsRequired();
        builder.Property(item => item.TicketTypeNameSnapshot).HasColumnName("ticket_type_name_snapshot").HasMaxLength(100).IsRequired();
        builder.Property(item => item.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
        builder.Property(item => item.Quantity).HasColumnName("quantity").IsRequired();
        builder.HasIndex(item => new { item.ReservationId, item.TicketTypeId })
            .IsUnique()
            .HasDatabaseName("reservation_items_reservation_ticket_type_key");
        builder.HasAlternateKey(item => new { item.Id, item.TicketTypeId })
            .HasName("reservation_items_id_ticket_type_key");
        builder.HasMany(item => item.Tickets)
            .WithOne(ticket => ticket.ReservationItem)
            .HasForeignKey(ticket => new { ticket.ReservationItemId, ticket.TicketTypeId })
            .HasPrincipalKey(item => new { item.Id, item.TicketTypeId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
