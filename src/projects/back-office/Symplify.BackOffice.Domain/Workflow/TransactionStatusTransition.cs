using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Domain.Workflow;

public class TransactionStatusTransition : Entity<int>, IEntityTimestamps, IAuditable
{
    public int FromStatusId { get; set; }

    public int ToStatusId { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual TransactionStatus FromStatus { get; set; } = null!;

    public virtual TransactionStatus ToStatus { get; set; } = null!;

    public virtual ICollection<TransactionStatusTransitionTranslation> Translations { get; set; } =
        new HashSet<TransactionStatusTransitionTranslation>();

    public virtual ICollection<WorkflowTemplateTransition> WorkflowTemplateTransitions { get; set; } =
        new HashSet<WorkflowTemplateTransition>();

    public virtual ICollection<CongressTransactionStatusTransition> CongressTransitions { get; set; } =
        new HashSet<CongressTransactionStatusTransition>();
}
