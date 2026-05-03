using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Workflow;

public class TransactionStatusPhase : Entity<int>, IEntityTimestamps, IAuditable
{
    public string Code { get; set; } = null!;

    public int Order { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<TransactionStatusPhaseTranslation> Translations { get; set; } =
        new HashSet<TransactionStatusPhaseTranslation>();

    public virtual ICollection<TransactionStatus> TransactionStatuses { get; set; } =
        new HashSet<TransactionStatus>();
}
