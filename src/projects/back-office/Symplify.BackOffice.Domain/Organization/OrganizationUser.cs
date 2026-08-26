using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Identity;

namespace Symplify.BackOffice.Domain.Organization;

public class OrganizationUser : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DefaultCongressId { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Organization Organization { get; set; } = null!;
    public virtual AppUser User { get; set; } = null!;
    public virtual Symplify.BackOffice.Domain.Congress.Congress? DefaultCongress { get; set; }
}
