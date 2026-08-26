using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressPaymentPlanTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressPaymentPlanId { get; set; }
    public Guid LanguageId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public virtual CongressPaymentPlan CongressPaymentPlan { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}
