using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Submission;

public sealed class SubmissionAcceptanceLetter : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid SubmissionId { get; set; }

    public Guid? LanguageId { get; set; }

    public Guid? AuthorId { get; set; }

    public string LetterNumber { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string AuthorFullNameSnapshot { get; set; } = string.Empty;

    public string? AuthorEmailSnapshot { get; set; }

    public Guid? SignerBoardMemberId { get; set; }

    public string? SignerNameSnapshot { get; set; }

    public string? SignerTitleSnapshot { get; set; }

    public string HtmlSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// Legacy/display compatible value. For object storage this value mirrors PdfObjectName.
    /// </summary>
    public string? PdfFilePath { get; set; }

    public string? StorageProvider { get; set; }

    public string? PdfBucketName { get; set; }

    public string? PdfObjectName { get; set; }

    public string? PdfContentType { get; set; }

    public long? PdfFileSize { get; set; }

    public string? PdfETag { get; set; }

    public DateTime GeneratedAt { get; set; }

    public DateTime? SentAt { get; set; }

    public string? SentToEmail { get; set; }

    public Submission Submission { get; set; } = null!;

    public Language? Language { get; set; }

    public Author? Author { get; set; }
}
