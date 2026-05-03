using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Workflow;

public class TransactionStatusPhaseTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public int TransactionStatusPhaseId { get; set; }

    public Guid LanguageId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual TransactionStatusPhase TransactionStatusPhase { get; set; } = null!;

    public virtual Language Language { get; set; } = null!;
}
