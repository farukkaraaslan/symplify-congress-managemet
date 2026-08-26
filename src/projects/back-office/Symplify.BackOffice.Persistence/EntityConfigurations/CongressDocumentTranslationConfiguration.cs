using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressDocumentTranslation;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressDocumentTranslationConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressDocumentTranslations");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnName("Id").IsRequired();
        builder.Property(entity => entity.CongressDocumentId).IsRequired();
        builder.Property(entity => entity.LanguageId).IsRequired();
        builder.Property(entity => entity.Description).HasColumnType("text");

        builder.HasIndex(entity => new { entity.CongressDocumentId, entity.LanguageId })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasIndex(entity => entity.LanguageId);

        builder.HasOne(entity => entity.CongressDocument)
            .WithMany(entity => entity.Translations)
            .HasForeignKey(entity => entity.CongressDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Language)
            .WithMany()
            .HasForeignKey(entity => entity.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
