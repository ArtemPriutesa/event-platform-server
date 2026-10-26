using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Persistence.Configurations;

public sealed class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
{
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
        builder.ToTable("ticket_types", table =>
        {
            table.HasCheckConstraint("ticket_types_price_check", "price >= 0");
            table.HasCheckConstraint("ticket_types_total_quantity_check", "total_quantity >= 0");
            table.HasCheckConstraint("ticket_types_reserved_quantity_check", "reserved_quantity >= 0");
            table.HasCheckConstraint("ticket_types_sold_quantity_check", "sold_quantity >= 0");
            table.HasCheckConstraint(
                "ticket_types_inventory_check",
                "reserved_quantity + sold_quantity <= total_quantity");
            table.HasCheckConstraint("ticket_types_version_check", "version > 0");
        });
        AuditableEntityConfiguration.Configure(builder);
        builder.Property(ticketType => ticketType.EventPublicId).HasColumnName("event_public_id").IsRequired();
        builder.Property(ticketType => ticketType.EventTitleSnapshot)
            .HasColumnName("event_title_snapshot").HasMaxLength(250).IsRequired();
        builder.Property(ticketType => ticketType.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(ticketType => ticketType.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(ticketType => ticketType.Price).HasColumnName("price").HasPrecision(18, 2);
        builder.Property(ticketType => ticketType.Currency).HasColumnName("currency").HasColumnType("char(3)").HasDefaultValue("UAH");
        builder.Property(ticketType => ticketType.TotalQuantity).HasColumnName("total_quantity");
        builder.Property(ticketType => ticketType.ReservedQuantity).HasColumnName("reserved_quantity").HasDefaultValue(0);
        builder.Property(ticketType => ticketType.SoldQuantity).HasColumnName("sold_quantity").HasDefaultValue(0);
        builder.Ignore(ticketType => ticketType.AvailableQuantity);
        builder.HasIndex(ticketType => new { ticketType.EventPublicId, ticketType.Name })
            .IsUnique()
            .HasDatabaseName("ticket_types_event_name_key");
        builder.HasIndex(ticketType => ticketType.EventPublicId)
            .HasDatabaseName("ticket_types_event_active_idx")
            .HasFilter("is_deleted = FALSE")
            .IncludeProperties(ticketType => new
            {
                ticketType.Name,
                ticketType.Price,
                ticketType.Currency,
                ticketType.TotalQuantity,
                ticketType.ReservedQuantity,
                ticketType.SoldQuantity
            });
        builder.HasMany(ticketType => ticketType.ReservationItems)
            .WithOne(item => item.TicketType)
            .HasForeignKey(item => item.TicketTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
