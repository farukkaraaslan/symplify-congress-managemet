using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Workflow;

public class PaymentStatusTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public int PaymentStatusId { get; set; }
    public Guid LanguageId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public virtual PaymentStatus PaymentStatus { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}
