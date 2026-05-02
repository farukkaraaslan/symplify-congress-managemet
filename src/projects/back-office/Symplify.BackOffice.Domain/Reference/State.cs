using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Reference.Translations;

namespace Symplify.BackOffice.Domain.Reference;

public class State : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CountryId { get; set; }
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;
    public int Order { get; set; }

    public virtual Country Country { get; set; } = null!;
    public virtual ICollection<City> Cities { get; set; } = new HashSet<City>();
    public virtual ICollection<StateTranslation> Translations { get; set; } = new HashSet<StateTranslation>();
}
