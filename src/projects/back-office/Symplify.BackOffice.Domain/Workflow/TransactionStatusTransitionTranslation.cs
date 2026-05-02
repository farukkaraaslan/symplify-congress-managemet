using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Workflow;

public class TransactionStatusTransitionTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public int TransactionStatusTransitionId { get; set; }
    public Guid LanguageId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public virtual TransactionStatusTransition TransactionStatusTransition { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}
