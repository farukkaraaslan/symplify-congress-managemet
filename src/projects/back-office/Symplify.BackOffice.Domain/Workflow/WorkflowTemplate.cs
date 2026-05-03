using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Common;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Domain.Workflow;

public class WorkflowTemplate : Entity<Guid>, IEntityTimestamps, IAuditable, IAggregateRoot
{
    public string Code { get; set; } = null!;

    public int? InitialTransactionStatusId { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual TransactionStatus? InitialTransactionStatus { get; set; }

    public virtual ICollection<WorkflowTemplateTranslation> Translations { get; set; } =
        new HashSet<WorkflowTemplateTranslation>();

    public virtual ICollection<WorkflowTemplateTransition> Transitions { get; set; } =
        new HashSet<WorkflowTemplateTransition>();

    public virtual ICollection<CongressWorkflowSetting> CongressWorkflowSettings { get; set; } =
        new HashSet<CongressWorkflowSetting>();
}
