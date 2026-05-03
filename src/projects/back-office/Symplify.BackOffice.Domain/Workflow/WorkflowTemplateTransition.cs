using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Domain.Workflow;

public class WorkflowTemplateTransition : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid WorkflowTemplateId { get; set; }

    public int TransactionStatusTransitionId { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual WorkflowTemplate WorkflowTemplate { get; set; } = null!;

    public virtual TransactionStatusTransition TransactionStatusTransition { get; set; } = null!;

    public virtual ICollection<CongressTransactionStatusTransition> CongressTransitions { get; set; } =
        new HashSet<CongressTransactionStatusTransition>();
}
