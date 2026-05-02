using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Identity;

namespace Symplify.BackOffice.Domain.Submission;

public class Reviewer : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid UserId { get; set; }
    public ReviewerStatus Status { get; set; } = ReviewerStatus.Pending;
    public bool IsActive { get; set; } = true;

    public virtual AppUser User { get; set; } = null!;
    public virtual ICollection<Submission> Submissions { get; set; } = new HashSet<Submission>();
    public virtual ICollection<SubmissionEvaluation> Evaluations { get; set; } = new HashSet<SubmissionEvaluation>();
}
