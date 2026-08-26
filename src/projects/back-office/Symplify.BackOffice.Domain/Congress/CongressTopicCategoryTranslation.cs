using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressTopicCategoryTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressTopicCategoryId { get; set; }
    public Guid LanguageId { get; set; }
    public string Name { get; set; } = null!;

    public virtual CongressTopicCategory CongressTopicCategory { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}
