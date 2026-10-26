using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Persistence.Configurations;

public sealed class ReservationDetailsConfiguration : IEntityTypeConfiguration<ReservationDetails>
{
    public void Configure(EntityTypeBuilder<ReservationDetails> builder)
    {
        builder.ToTable("reservation_details", table =>
            table.HasCheckConstraint("reservation_details_version_check", "version > 0"));
        AuditableEntityConfiguration.Configure(builder, generateIdentity: false);
        builder.Property(details => details.ContactNameSnapshot).HasColumnName("contact_name_snapshot").HasMaxLength(200).IsRequired();
        builder.Property(details => details.ContactEmailSnapshot).HasColumnName("contact_email_snapshot").HasMaxLength(320).IsRequired();
        builder.Property(details => details.Notes).HasColumnName("notes").HasMaxLength(1000);
    }
}
