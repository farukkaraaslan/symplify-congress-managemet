using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Reference.Translations;

public class CityTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CityId { get; set; }
    public Guid LanguageId { get; set; }
    public string Name { get; set; } = null!;

    public virtual Symplify.BackOffice.Domain.Reference.City City { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}
