using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Reference.Translations;

namespace Symplify.BackOffice.Domain.Reference;

public class Region : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid? CountryId { get; set; }
    public bool IsActive { get; set; } = true;
    public int Order { get; set; }

    public virtual Country? Country { get; set; }
    public virtual ICollection<RegionTranslation> Translations { get; set; } = new HashSet<RegionTranslation>();
}
