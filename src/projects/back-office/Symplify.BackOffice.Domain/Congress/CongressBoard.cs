using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressBoard : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Congress Congress { get; set; } = null!;
    public virtual ICollection<CongressBoardTranslation> Translations { get; set; } = new HashSet<CongressBoardTranslation>();
    public virtual ICollection<CongressBoardMember> Members { get; set; } = new HashSet<CongressBoardMember>();
}
