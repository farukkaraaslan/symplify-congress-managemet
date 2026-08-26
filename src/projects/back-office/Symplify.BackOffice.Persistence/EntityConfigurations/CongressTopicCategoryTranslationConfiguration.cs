using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressTopicCategoryTranslation;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressTopicCategoryTranslationConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressTopicCategoryTranslations");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CongressTopicCategoryId).IsRequired();
        builder.Property(entity => entity.LanguageId).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();

        builder.HasIndex(entity => new { entity.CongressTopicCategoryId, entity.LanguageId })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL")
            .HasDatabaseName("IX_CongressTopicCategoryTranslations_CategoryId_LanguageId");

        builder.HasOne(entity => entity.CongressTopicCategory)
            .WithMany(category => category.Translations)
            .HasForeignKey(entity => entity.CongressTopicCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Language)
            .WithMany()
            .HasForeignKey(entity => entity.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
