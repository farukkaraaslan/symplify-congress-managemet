using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Lookups;

public class TopicTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid TopicId { get; set; }
    public Guid LanguageId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public virtual Topic Topic { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}
