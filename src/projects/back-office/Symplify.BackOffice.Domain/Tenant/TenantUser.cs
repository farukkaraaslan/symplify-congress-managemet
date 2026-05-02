using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Identity;

namespace Symplify.BackOffice.Domain.Tenant;

public class TenantUser : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid? DefaultCongressId { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Tenant Tenant { get; set; } = null!;
    public virtual AppUser User { get; set; } = null!;
    public virtual Symplify.BackOffice.Domain.Congress.Congress? DefaultCongress { get; set; }
}
