using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressImportantDate : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }
    public DateTime Date { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Congress Congress { get; set; } = null!;
    public virtual ICollection<CongressImportantDateTranslation> Translations { get; set; } = new HashSet<CongressImportantDateTranslation>();
}
