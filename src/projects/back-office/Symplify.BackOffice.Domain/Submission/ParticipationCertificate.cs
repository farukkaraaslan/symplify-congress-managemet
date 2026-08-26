using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Submission;

public sealed class ParticipationCertificate : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }

    public Guid SubmissionId { get; set; }

    public Guid AuthorId { get; set; }

    public Guid TemplateId { get; set; }

    public string Culture { get; set; } = "tr-TR";

    public string SubmissionNumber { get; set; } = string.Empty;

    public string SubmissionTitleSnapshot { get; set; } = string.Empty;

    public string AuthorFullNameSnapshot { get; set; } = string.Empty;

    public string? AuthorEmailSnapshot { get; set; }

    public string? AuthorInstitutionSnapshot { get; set; }

    public bool IsVideoPresentation { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string? StorageProvider { get; set; }

    public string BucketName { get; set; } = string.Empty;

    public string ObjectName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/pdf";

    public long? FileSize { get; set; }

    public string? ETag { get; set; }

    public DateTime GeneratedAt { get; set; }

    public DateTime? EmailQueuedAt { get; set; }

    public DateTime? EmailSentAt { get; set; }

    public string? EmailStatus { get; set; }

    public string? EmailError { get; set; }

    /// <summary>
    /// Non-sequential public identifier used in anonymous certificate links.
    /// </summary>
    public Guid? PublicId { get; set; }

    /// <summary>
    /// SHA-256 hash of the public access token. The raw token is never persisted.
    /// </summary>
    public string? PublicAccessTokenHash { get; set; }

    /// <summary>
    /// Public link becomes available only after the certificate email is sent successfully.
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public Guid? RevokedByUserId { get; set; }

    public string? RevocationReason { get; set; }

    public Symplify.BackOffice.Domain.Congress.Congress Congress { get; set; } = null!;

    public Submission Submission { get; set; } = null!;

    public Author Author { get; set; } = null!;

    public ParticipationCertificateTemplate Template { get; set; } = null!;
}
