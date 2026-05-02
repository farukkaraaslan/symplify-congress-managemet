using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressSection : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }
    public string BindingKey { get; set; } = null!;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Congress Congress { get; set; } = null!;
    public virtual ICollection<CongressSectionTranslation> Translations { get; set; } = new HashSet<CongressSectionTranslation>();
}
