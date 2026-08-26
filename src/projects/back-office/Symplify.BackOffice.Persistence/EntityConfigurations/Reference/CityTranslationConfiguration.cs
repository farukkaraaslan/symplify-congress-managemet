using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Reference.Translations.CityTranslation;

namespace Symplify.BackOffice.Persistence.EntityConfigurations.Reference;

public sealed class CityTranslationConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CityTranslations");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("Id").IsRequired();
    }
}
