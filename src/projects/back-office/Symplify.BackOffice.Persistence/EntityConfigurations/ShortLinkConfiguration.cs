using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.ShortLinks;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class ShortLinkConfiguration : IEntityTypeConfiguration<ShortLink>
{
    public void Configure(EntityTypeBuilder<ShortLink> builder)
    {
        builder.ToTable("ShortLinks");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Code).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.TargetType).HasConversion<int>().IsRequired();
        builder.Property(entity => entity.TargetId).IsRequired();
        builder.Property(entity => entity.Culture).HasMaxLength(10);
        builder.Property(entity => entity.ClickCount).IsRequired().HasDefaultValue(0);
        builder.Property(entity => entity.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(entity => entity.Code)
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasIndex(entity => new { entity.TargetType, entity.TargetId })
            .HasFilter("\"DeletedDate\" IS NULL AND \"IsActive\"");

        builder.HasQueryFilter(entity => entity.DeletedDate == null);
    }
}
