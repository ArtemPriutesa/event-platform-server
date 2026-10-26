using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Dal.Persistence.Configurations;

internal static class AuditableEntityConfiguration
{
    public static void Configure<TEntity>(EntityTypeBuilder<TEntity> builder, bool generateIdentity = true)
        where TEntity : AuditableEntity
    {
        builder.HasKey(entity => entity.Id);
        var id = builder.Property(entity => entity.Id)
            .HasColumnName(generateIdentity ? "id" : "reservation_id");
        if (generateIdentity)
        {
            id.UseIdentityAlwaysColumn();
        }
        else
        {
            id.ValueGeneratedNever();
        }
        builder.Property(entity => entity.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(entity => entity.CreatedBy).HasColumnName("created_by").HasDefaultValueSql("CURRENT_USER");
        builder.Property(entity => entity.UpdatedAt).HasColumnName("updated_at");
        builder.Property(entity => entity.UpdatedBy).HasColumnName("updated_by");
        builder.Property(entity => entity.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(entity => entity.Version).HasColumnName("version").HasDefaultValue(1L).IsConcurrencyToken();
        builder.HasIndex(entity => entity.IsDeleted);
        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}
