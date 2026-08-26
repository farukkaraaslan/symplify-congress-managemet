using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntityType = Symplify.BackOffice.Domain.Submission.Submission;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class SubmissionConfiguration : IEntityTypeConfiguration<EntityType>
{
    public void Configure(EntityTypeBuilder<EntityType> builder)
    {
        builder.ToTable("Submissions");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("Id").IsRequired();

        builder.Property(entity => entity.SubmissionNumber)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(entity => entity.Orcid)
            .HasMaxLength(50);

        builder.Property(entity => entity.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(entity => entity.TitleEn)
            .HasMaxLength(300);

        builder.Property(entity => entity.Keywords)
            .HasMaxLength(500);

        builder.Property(entity => entity.KeywordsEn)
            .HasMaxLength(500);

        builder.HasIndex(entity => new { entity.CongressId, entity.SubmissionNumber })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasOne(entity => entity.Congress)
            .WithMany()
            .HasForeignKey(entity => entity.CongressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.SubmissionType)
            .WithMany()
            .HasForeignKey(entity => entity.SubmissionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Topic)
            .WithMany()
            .HasForeignKey(entity => entity.TopicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.CreatedByUser)
            .WithMany()
            .HasForeignKey(entity => entity.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Language)
            .WithMany()
            .HasForeignKey(entity => entity.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.PaymentStatus)
            .WithMany()
            .HasForeignKey(entity => entity.PaymentStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.TransactionStatus)
            .WithMany()
            .HasForeignKey(entity => entity.TransactionStatusId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
