using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressSliderTranslation;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressSliderTranslationConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressSliderTranslations");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(entity => entity.CongressSliderId)
            .IsRequired();

        builder.Property(entity => entity.LanguageId)
            .IsRequired();

        builder.Property(entity => entity.Title)
            .HasMaxLength(300);

        builder.Property(entity => entity.Subtitle)
            .HasMaxLength(1000);

        builder.Property(entity => entity.ButtonText)
            .HasMaxLength(120);

        builder.Property(entity => entity.ButtonUrl)
            .HasMaxLength(1000);

        builder.HasIndex(entity => new { entity.CongressSliderId, entity.LanguageId })
            .IsUnique();

        builder.HasOne(entity => entity.CongressSlider)
            .WithMany(slider => slider.Translations)
            .HasForeignKey(entity => entity.CongressSliderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Language)
            .WithMany()
            .HasForeignKey(entity => entity.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
