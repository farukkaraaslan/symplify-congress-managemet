using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Domain.Submission;

public sealed class SubmissionFile : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid SubmissionId { get; set; }

    public SubmissionFileKind FileKind { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public long? FileSize { get; set; }

    public SubmissionFileReviewStatus ReviewStatus { get; set; } = SubmissionFileReviewStatus.PendingReview;

    public Guid? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    public bool IsIncludedInProgramBook { get; set; }

    public int VersionNo { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public Submission Submission { get; set; } = null!;
}
