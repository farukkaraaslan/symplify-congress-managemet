using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Organization.OrganizationUser;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class OrganizationUserConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("OrganizationUsers");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("Id").IsRequired();
    }
}
