using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Organization.Organization;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("Organizations");

        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("Id").IsRequired();

        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Code).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.Slug).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.ShortName).HasMaxLength(80).IsRequired();

        builder.Property(entity => entity.WebsiteUrl).HasMaxLength(500);
        builder.Property(entity => entity.HostUrl).HasMaxLength(500);
        builder.Property(entity => entity.Description).HasMaxLength(1000);

        builder.Property(entity => entity.ContactName).HasMaxLength(200);
        builder.Property(entity => entity.ContactTitle).HasMaxLength(200);
        builder.Property(entity => entity.ContactEmail).HasMaxLength(256);
        builder.Property(entity => entity.ContactPhone).HasMaxLength(50);
        builder.Property(entity => entity.ContactNote).HasMaxLength(1000);

        builder.Property(entity => entity.LogoLightPath).HasMaxLength(500);
        builder.Property(entity => entity.LogoDarkPath).HasMaxLength(500);
        builder.Property(entity => entity.BrandColor).HasMaxLength(20);
        builder.Property(entity => entity.IsActive).HasDefaultValue(true).IsRequired();

        builder.HasIndex(entity => entity.Code).IsUnique();
        builder.HasIndex(entity => entity.Slug).IsUnique();
    }
}
