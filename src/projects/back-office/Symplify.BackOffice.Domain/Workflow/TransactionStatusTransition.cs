using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Workflow;

public class TransactionStatusTransition : Entity<int>, IEntityTimestamps, IAuditable
{
    public int FromStatusId { get; set; }
    public int ToStatusId { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual TransactionStatus FromStatus { get; set; } = null!;
    public virtual TransactionStatus ToStatus { get; set; } = null!;
    public virtual ICollection<TransactionStatusTransitionTranslation> Translations { get; set; } = new HashSet<TransactionStatusTransitionTranslation>();
}
