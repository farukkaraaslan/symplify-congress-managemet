using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Congress.CongressBoardMember;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class CongressBoardMemberConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("CongressBoardMembers");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(entity => entity.FullName)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(entity => entity.AcademicTitle)
            .HasMaxLength(100);

        builder.Property(entity => entity.Institution)
            .HasMaxLength(500);

        builder.Property(entity => entity.ImagePath)
            .HasMaxLength(1000);

        builder.Property(entity => entity.ImageStorageProvider)
            .HasMaxLength(50);

        builder.Property(entity => entity.ImageBucketName)
            .HasMaxLength(150);

        builder.Property(entity => entity.ImageObjectName)
            .HasMaxLength(1000);

        builder.Property(entity => entity.ImageFileName)
            .HasMaxLength(255);

        builder.Property(entity => entity.ImageContentType)
            .HasMaxLength(150);

        builder.Property(entity => entity.ImageETag)
            .HasMaxLength(250);

        builder.Property(entity => entity.ImageFileSize);

        builder.Property(entity => entity.IsAcceptanceLetterSigner)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(entity => entity.SignaturePath)
            .HasMaxLength(1000);

        builder.Property(entity => entity.SignatureStorageProvider)
            .HasMaxLength(50);

        builder.Property(entity => entity.SignatureBucketName)
            .HasMaxLength(150);

        builder.Property(entity => entity.SignatureObjectName)
            .HasMaxLength(1000);

        builder.Property(entity => entity.SignatureFileName)
            .HasMaxLength(255);

        builder.Property(entity => entity.SignatureContentType)
            .HasMaxLength(150);

        builder.Property(entity => entity.SignatureETag)
            .HasMaxLength(250);

        builder.Property(entity => entity.SignatureFileSize);

        builder.Property(entity => entity.Order)
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .IsRequired();

        builder.HasOne(entity => entity.CongressBoard)
            .WithMany(board => board.Members)
            .HasForeignKey(entity => entity.CongressBoardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.CongressBoardId, entity.Order });
        builder.HasIndex(entity => entity.IsAcceptanceLetterSigner);
    }
}
