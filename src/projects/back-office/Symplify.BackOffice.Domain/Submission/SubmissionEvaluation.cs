using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Submission;

public class SubmissionEvaluation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid SubmissionId { get; set; }
    public Guid ReviewerId { get; set; }
    public string? Comment { get; set; }
    public string? Recommendation { get; set; }
    public decimal? TotalScore { get; set; }
    public DateTime? CompletedAt { get; set; }

    public virtual Submission Submission { get; set; } = null!;
    public virtual Reviewer Reviewer { get; set; } = null!;
    public virtual ICollection<EvaluationScore> Scores { get; set; } = new HashSet<EvaluationScore>();
}
