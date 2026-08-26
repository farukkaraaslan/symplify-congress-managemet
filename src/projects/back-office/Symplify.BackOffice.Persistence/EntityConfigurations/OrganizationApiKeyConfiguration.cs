using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Organization.OrganizationApiKey;
namespace Symplify.BackOffice.Persistence.EntityConfigurations;
public sealed class OrganizationApiKeyConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("OrganizationApiKeys");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrganizationId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(40).IsRequired();
        builder.Property(x => x.KeyType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.KeyPrefix).HasMaxLength(80).IsRequired();
        builder.Property(x => x.KeyHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Scopes).HasMaxLength(1000).HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.AllowedIpAddresses).HasMaxLength(2000);
        builder.Property(x => x.AllowedDomains).HasMaxLength(2000);
        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.KeyPrefix).IsUnique();
        builder.HasOne(x => x.Organization).WithMany(x => x.ApiKeys).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
    }
}
