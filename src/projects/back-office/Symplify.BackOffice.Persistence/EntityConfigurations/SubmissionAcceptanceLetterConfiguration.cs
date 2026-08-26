using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Persistence.EntityConfigurations;

public sealed class SubmissionAcceptanceLetterConfiguration : IEntityTypeConfiguration<SubmissionAcceptanceLetter>
{
    public void Configure(EntityTypeBuilder<SubmissionAcceptanceLetter> builder)
    {
        builder.ToTable("SubmissionAcceptanceLetters");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.LetterNumber).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.FileName).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.AuthorFullNameSnapshot).HasMaxLength(250).IsRequired();
        builder.Property(entity => entity.AuthorEmailSnapshot).HasMaxLength(250);
        builder.Property(entity => entity.SignerNameSnapshot).HasMaxLength(250);
        builder.Property(entity => entity.SignerTitleSnapshot).HasMaxLength(250);
        builder.Property(entity => entity.HtmlSnapshot).IsRequired();
        builder.Property(entity => entity.PdfFilePath).HasMaxLength(1000);
        builder.Property(entity => entity.StorageProvider).HasMaxLength(100);
        builder.Property(entity => entity.PdfBucketName).HasMaxLength(150);
        builder.Property(entity => entity.PdfObjectName).HasMaxLength(1000);
        builder.Property(entity => entity.PdfContentType).HasMaxLength(150);
        builder.Property(entity => entity.PdfETag).HasMaxLength(250);
        builder.Property(entity => entity.SentToEmail).HasMaxLength(250);
        builder.Property(entity => entity.GeneratedAt).IsRequired();

        builder.HasOne(entity => entity.Submission)
            .WithMany(submission => submission.AcceptanceLetters)
            .HasForeignKey(entity => entity.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Language)
            .WithMany()
            .HasForeignKey(entity => entity.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Author)
            .WithMany()
            .HasForeignKey(entity => entity.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => new { entity.SubmissionId, entity.AuthorId, entity.LanguageId })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasIndex(entity => new { entity.SubmissionId, entity.LanguageId, entity.LetterNumber })
            .IsUnique()
            .HasFilter("\"DeletedDate\" IS NULL");

        builder.HasQueryFilter(entity => entity.DeletedDate == null && entity.Submission.DeletedDate == null);
    }
}
