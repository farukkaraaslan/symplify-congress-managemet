using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressTransactionStatusTransition : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }

    public int TransactionStatusTransitionId { get; set; }

    public Guid? SourceWorkflowTemplateTransitionId { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Congress Congress { get; set; } = null!;

    public virtual TransactionStatusTransition TransactionStatusTransition { get; set; } = null!;

    public virtual WorkflowTemplateTransition? SourceWorkflowTemplateTransition { get; set; }
}
