using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Reference.Translations;

namespace Symplify.BackOffice.Domain.Reference;

public class Country : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public string? Code { get; set; }
    public string? PhoneCode { get; set; }
    public bool IsActive { get; set; } = true;
    public int Order { get; set; }

    public virtual ICollection<State> States { get; set; } = new HashSet<State>();
    public virtual ICollection<Region> Regions { get; set; } = new HashSet<Region>();
    public virtual ICollection<CountryTranslation> Translations { get; set; } = new HashSet<CountryTranslation>();
}
