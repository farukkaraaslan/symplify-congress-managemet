using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Reference.Translations;

public class StateTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid StateId { get; set; }
    public Guid LanguageId { get; set; }
    public string Name { get; set; } = null!;

    public virtual Symplify.BackOffice.Domain.Reference.State State { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}
