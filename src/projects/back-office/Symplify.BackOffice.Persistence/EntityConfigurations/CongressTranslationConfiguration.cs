using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressTranslation;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressTranslationConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressTranslations");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(entity => entity.CongressId)
            .IsRequired();

        builder.Property(entity => entity.LanguageId)
            .IsRequired();

        builder.Property(entity => entity.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(entity => entity.Subtitle)
            .HasMaxLength(500);

        builder.Property(entity => entity.ShortDescription)
            .HasMaxLength(1000);

        builder.Property(entity => entity.Description)
            .HasColumnType("text");

        builder.Property(entity => entity.WelcomeTitle)
            .HasMaxLength(300);

        builder.Property(entity => entity.WelcomeContent)
            .HasColumnType("text");

        builder.Property(entity => entity.SeoTitle)
            .HasMaxLength(300);

        builder.Property(entity => entity.SeoDescription)
            .HasMaxLength(500);

        builder.Property(entity => entity.LogoPath)
            .HasMaxLength(500);

        builder.HasIndex(entity => new { entity.CongressId, entity.LanguageId })
            .IsUnique();

        builder.HasOne(entity => entity.Congress)
            .WithMany(congress => congress.Translations)
            .HasForeignKey(entity => entity.CongressId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Language)
            .WithMany()
            .HasForeignKey(entity => entity.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
