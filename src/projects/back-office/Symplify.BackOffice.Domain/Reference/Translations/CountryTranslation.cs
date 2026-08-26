using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Localization;

namespace Symplify.BackOffice.Domain.Reference.Translations;

public class CountryTranslation : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CountryId { get; set; }
    public Guid LanguageId { get; set; }
    public string Name { get; set; } = null!;

    public virtual Symplify.BackOffice.Domain.Reference.Country Country { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}
