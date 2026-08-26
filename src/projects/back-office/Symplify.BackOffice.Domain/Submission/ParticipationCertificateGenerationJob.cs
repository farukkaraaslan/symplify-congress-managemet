using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Domain.Submission;

public sealed class ParticipationCertificateGenerationJob : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }
    public string Culture { get; set; } = "tr-TR";
    public string? SubmissionStatusCode { get; set; }
    public string? PaymentStatusCode { get; set; }
    public string? CandidateSearch { get; set; }
    public bool SelectAllFiltered { get; set; }
    public string SelectedCandidateKeysJson { get; set; } = "[]";
    public string ExcludedCandidateKeysJson { get; set; } = "[]";
    public int ExcludedCount { get; set; }
    public ParticipationCertificateGenerationJobStatus Status { get; set; } = ParticipationCertificateGenerationJobStatus.Pending;
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; }
    public int SucceededCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? MaterializedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? HeartbeatAt { get; set; }
    public string? LastError { get; set; }
    public Guid? RequestedByUserId { get; set; }

    public Symplify.BackOffice.Domain.Congress.Congress Congress { get; set; } = null!;
    public ICollection<ParticipationCertificateGenerationJobItem> Items { get; set; } = new List<ParticipationCertificateGenerationJobItem>();
}
