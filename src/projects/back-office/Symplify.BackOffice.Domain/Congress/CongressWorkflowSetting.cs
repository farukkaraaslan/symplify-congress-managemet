using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressWorkflowSetting : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }

    public Guid? SourceWorkflowTemplateId { get; set; }

    public int? InitialTransactionStatusId { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual Congress Congress { get; set; } = null!;

    public virtual WorkflowTemplate? SourceWorkflowTemplate { get; set; }

    public virtual TransactionStatus? InitialTransactionStatus { get; set; }
}
