using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Workflow;

public class TransactionStatus : Entity<int>, IEntityTimestamps, IAuditable
{
    public int TransactionStatusPhaseId { get; set; }

    public string Code { get; set; } = null!;

    public int Order { get; set; }

    public bool IsEditable { get; set; } = true;

    public bool IsFinal { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual TransactionStatusPhase TransactionStatusPhase { get; set; } = null!;

    public virtual ICollection<TransactionStatusTranslation> Translations { get; set; } =
        new HashSet<TransactionStatusTranslation>();

    public virtual ICollection<TransactionStatusTransition> FromTransitions { get; set; } =
        new HashSet<TransactionStatusTransition>();

    public virtual ICollection<TransactionStatusTransition> ToTransitions { get; set; } =
        new HashSet<TransactionStatusTransition>();
}
