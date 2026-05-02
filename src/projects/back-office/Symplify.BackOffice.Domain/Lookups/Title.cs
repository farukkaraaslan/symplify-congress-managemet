using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Lookups;

public class Title : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public string? Code { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<TitleTranslation> Translations { get; set; } = new HashSet<TitleTranslation>();
    public virtual ICollection<Symplify.BackOffice.Domain.Identity.AppUser> Users { get; set; } = new HashSet<Symplify.BackOffice.Domain.Identity.AppUser>();
}
