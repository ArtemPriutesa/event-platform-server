using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Persistence.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets", table =>
        {
            table.HasCheckConstraint(
                "tickets_status_check",
                "status IN ('VALID', 'USED', 'REFUNDED')");
            table.HasCheckConstraint(
                "tickets_used_at_check",
                "(status = 'USED' AND used_at IS NOT NULL) OR (status <> 'USED' AND used_at IS NULL)");
            table.HasCheckConstraint("tickets_version_check", "version > 0");
        });
        AuditableEntityConfiguration.Configure(builder);
        builder.Property(ticket => ticket.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()");
        builder.HasIndex(ticket => ticket.PublicId).IsUnique();
        builder.Property(ticket => ticket.ReservationItemId).HasColumnName("reservation_item_id");
        builder.Property(ticket => ticket.TicketTypeId).HasColumnName("ticket_type_id");
        builder.Property(ticket => ticket.Status)
            .HasColumnName("status")
            .HasConversion(
                value => value.ToString().ToUpperInvariant(),
                value => Enum.Parse<TicketStatus>(value, true))
            .HasMaxLength(20)
            .HasDefaultValueSql("'VALID'");
        builder.Property(ticket => ticket.IssuedAt).HasColumnName("issued_at").HasDefaultValueSql("now()");
        builder.Property(ticket => ticket.UsedAt).HasColumnName("used_at");
        builder.HasIndex(ticket => new { ticket.ReservationItemId, ticket.Status })
            .HasDatabaseName("tickets_reservation_item_status_idx")
            .HasFilter("is_deleted = FALSE")
            .IncludeProperties(ticket => new { ticket.PublicId, ticket.IssuedAt, ticket.UsedAt });
    }
}
