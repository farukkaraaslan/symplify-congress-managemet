using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Identity;

namespace Symplify.BackOffice.Domain.Submission;

public class SubmissionHistory : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid SubmissionId { get; set; }
    public int? FromStatusId { get; set; }
    public int? ToStatusId { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? Note { get; set; }

    public virtual Submission Submission { get; set; } = null!;
    public virtual Symplify.BackOffice.Domain.Workflow.TransactionStatus? FromStatus { get; set; }
    public virtual Symplify.BackOffice.Domain.Workflow.TransactionStatus? ToStatus { get; set; }
    public virtual AppUser? PerformedByUser { get; set; }
}
