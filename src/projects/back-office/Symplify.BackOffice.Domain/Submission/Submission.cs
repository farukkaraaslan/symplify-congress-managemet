using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Common;
using Symplify.BackOffice.Domain.Identity;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Domain.Submission;

public class Submission : Entity<Guid>, IEntityTimestamps, IAuditable, IAggregateRoot
{
    public Guid CongressId { get; set; }
    public Guid? SubmissionTypeId { get; set; }
    public Guid? TopicId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public int? PaymentStatusId { get; set; }
    public int? TransactionStatusId { get; set; }

    public string Title { get; set; } = null!;
    public string? Abstract { get; set; }
    public string? Keywords { get; set; }

    public virtual Symplify.BackOffice.Domain.Congress.Congress Congress { get; set; } = null!;
    public virtual SubmissionType? SubmissionType { get; set; }
    public virtual Topic? Topic { get; set; }
    public virtual AppUser? CreatedByUser { get; set; }
    public virtual Symplify.BackOffice.Domain.Workflow.PaymentStatus? PaymentStatus { get; set; }
    public virtual Symplify.BackOffice.Domain.Workflow.TransactionStatus? TransactionStatus { get; set; }

    public virtual ICollection<Author> Authors { get; set; } = new HashSet<Author>();
    public virtual ICollection<Reviewer> Reviewers { get; set; } = new HashSet<Reviewer>();
    public virtual ICollection<SubmissionEvaluation> Evaluations { get; set; } = new HashSet<SubmissionEvaluation>();
    public virtual ICollection<SubmissionHistory> Histories { get; set; } = new HashSet<SubmissionHistory>();
}
