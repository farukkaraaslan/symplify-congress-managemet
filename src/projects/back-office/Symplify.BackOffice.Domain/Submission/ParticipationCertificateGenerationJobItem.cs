using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Domain.Submission;

public sealed class ParticipationCertificateGenerationJobItem : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid JobId { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid AuthorId { get; set; }
    public string SubmissionNumber { get; set; } = string.Empty;
    public string SubmissionTitle { get; set; } = string.Empty;
    public string SubmissionTypeName { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string? AuthorEmail { get; set; }
    public string? AuthorInstitution { get; set; }
    public bool IsVideoPresentation { get; set; }
    public ParticipationCertificateGenerationItemStatus Status { get; set; } = ParticipationCertificateGenerationItemStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
    public Guid? CertificateId { get; set; }

    public ParticipationCertificateGenerationJob Job { get; set; } = null!;
}
