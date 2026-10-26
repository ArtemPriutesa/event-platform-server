using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations", table =>
        {
            table.HasCheckConstraint(
                "reservations_status_check",
                "status IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')");
            table.HasCheckConstraint("reservations_version_check", "version > 0");
        });
        AuditableEntityConfiguration.Configure(builder);
        builder.Property(reservation => reservation.PublicId).HasColumnName("public_id").HasDefaultValueSql("gen_random_uuid()");
        builder.HasIndex(reservation => reservation.PublicId).IsUnique();
        builder.Property(reservation => reservation.UserPublicId).HasColumnName("user_public_id").IsRequired();
        builder.Property(reservation => reservation.Status)
            .HasColumnName("status")
            .HasConversion(
                value => value.ToString().ToUpperInvariant(),
                value => Enum.Parse<ReservationStatus>(value, true))
            .HasMaxLength(20)
            .HasDefaultValueSql("'HELD'");
        builder.Property(reservation => reservation.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.HasIndex(reservation => new
            {
                reservation.UserPublicId,
                reservation.Status,
                reservation.CreatedAt
            })
            .HasDatabaseName("reservations_user_status_created_at_idx")
            .IsDescending(false, false, true)
            .HasFilter("is_deleted = FALSE")
            .IncludeProperties(reservation => new { reservation.PublicId, reservation.ExpiresAt });
        builder.HasOne(reservation => reservation.Details)
            .WithOne(details => details.Reservation)
            .HasForeignKey<ReservationDetails>(details => details.Id)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(reservation => reservation.Items)
            .WithOne(item => item.Reservation)
            .HasForeignKey(item => item.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
