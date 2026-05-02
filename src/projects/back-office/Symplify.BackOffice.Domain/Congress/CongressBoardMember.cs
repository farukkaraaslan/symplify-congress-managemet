using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressBoardMember : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressBoardId { get; set; }
    public string? ImagePath { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual CongressBoard CongressBoard { get; set; } = null!;
    public virtual ICollection<CongressBoardMemberTranslation> Translations { get; set; } = new HashSet<CongressBoardMemberTranslation>();
}
