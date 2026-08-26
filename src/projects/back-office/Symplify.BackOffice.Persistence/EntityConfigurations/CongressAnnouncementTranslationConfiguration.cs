using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressAnnouncementTranslation;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressAnnouncementTranslationConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressAnnouncementTranslations");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(entity => entity.CongressAnnouncementId)
            .IsRequired();

        builder.Property(entity => entity.LanguageId)
            .IsRequired();

        builder.Property(entity => entity.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(entity => entity.Summary)
            .HasMaxLength(1000);

        builder.Property(entity => entity.Content)
            .HasColumnType("text");

        builder.Property(entity => entity.SeoTitle)
            .HasMaxLength(300);

        builder.Property(entity => entity.SeoDescription)
            .HasMaxLength(500);

        builder.HasIndex(entity => new { entity.CongressAnnouncementId, entity.LanguageId })
            .IsUnique();

        builder.HasOne(entity => entity.CongressAnnouncement)
            .WithMany(announcement => announcement.Translations)
            .HasForeignKey(entity => entity.CongressAnnouncementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Language)
            .WithMany()
            .HasForeignKey(entity => entity.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
