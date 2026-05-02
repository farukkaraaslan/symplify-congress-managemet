using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Reference.Translations;

namespace Symplify.BackOffice.Domain.Reference;

public class City : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid StateId { get; set; }
    public bool IsActive { get; set; } = true;
    public int Order { get; set; }

    public virtual State State { get; set; } = null!;
    public virtual ICollection<CityTranslation> Translations { get; set; } = new HashSet<CityTranslation>();
}
