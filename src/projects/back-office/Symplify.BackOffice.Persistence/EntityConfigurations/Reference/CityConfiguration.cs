using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Reference.City;

namespace Symplify.BackOffice.Persistence.EntityConfigurations.Reference;

public sealed class CityConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("Citys");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("Id").IsRequired();
    }
}
