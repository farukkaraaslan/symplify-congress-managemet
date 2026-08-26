using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressDocument;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressDocumentConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressDocuments");
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id).HasColumnName("Id").IsRequired();
        builder.Property(entity => entity.CongressId).IsRequired();
        builder.Property(entity => entity.DocumentTypeId);
        builder.Property(entity => entity.FilePath).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.OriginalFileName).HasMaxLength(500);
        builder.Property(entity => entity.StorageProvider).HasMaxLength(50);
        builder.Property(entity => entity.BucketName).HasMaxLength(100);
        builder.Property(entity => entity.ObjectName).HasMaxLength(1000);
        builder.Property(entity => entity.ContentType).HasMaxLength(150);
        builder.Property(entity => entity.FileExtension).HasMaxLength(20);
        builder.Property(entity => entity.FileSize);
        builder.Property(entity => entity.ETag).HasMaxLength(200);
        builder.Property(entity => entity.CoverImagePath).HasMaxLength(1000);
        builder.Property(entity => entity.CoverImageStorageProvider).HasMaxLength(50);
        builder.Property(entity => entity.CoverImageBucketName).HasMaxLength(100);
        builder.Property(entity => entity.CoverImageObjectName).HasMaxLength(1000);
        builder.Property(entity => entity.CoverImageFileName).HasMaxLength(500);
        builder.Property(entity => entity.CoverImageContentType).HasMaxLength(150);
        builder.Property(entity => entity.CoverImageFileSize);
        builder.Property(entity => entity.CoverImageETag).HasMaxLength(200);
        builder.Property(entity => entity.Order).IsRequired();
        builder.Property(entity => entity.IsActive).IsRequired();

        builder.HasIndex(entity => new { entity.CongressId, entity.Order });
        builder.HasIndex(entity => entity.DocumentTypeId);
        builder.HasIndex(entity => entity.IsActive);
        builder.HasIndex(entity => new { entity.BucketName, entity.ObjectName })
            .HasFilter("\"BucketName\" IS NOT NULL AND \"ObjectName\" IS NOT NULL");

        builder.HasIndex(entity => new { entity.CoverImageBucketName, entity.CoverImageObjectName })
            .HasFilter("\"CoverImageBucketName\" IS NOT NULL AND \"CoverImageObjectName\" IS NOT NULL");

        builder.HasOne(entity => entity.Congress)
            .WithMany(congress => congress.Documents)
            .HasForeignKey(entity => entity.CongressId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.DocumentType)
            .WithMany()
            .HasForeignKey(entity => entity.DocumentTypeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
