using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Domain.Submission;

public class EvaluationScore : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid SubmissionEvaluationId { get; set; }
    public Guid EvaluationCriterionId { get; set; }
    public decimal Score { get; set; }
    public string? Comment { get; set; }

    public virtual SubmissionEvaluation SubmissionEvaluation { get; set; } = null!;
    public virtual EvaluationCriterion EvaluationCriterion { get; set; } = null!;
}
