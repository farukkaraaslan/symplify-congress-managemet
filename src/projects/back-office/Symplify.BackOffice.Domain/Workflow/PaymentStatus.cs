using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Workflow;

public class PaymentStatus : Entity<int>, IEntityTimestamps, IAuditable
{
    public string Code { get; set; } = null!;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<PaymentStatusTranslation> Translations { get; set; } = new HashSet<PaymentStatusTranslation>();
}
