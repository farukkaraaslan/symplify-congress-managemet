using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressPaymentPlan : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime? DueDate { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Congress Congress { get; set; } = null!;
    public virtual ICollection<CongressPaymentPlanTranslation> Translations { get; set; } = new HashSet<CongressPaymentPlanTranslation>();
}
