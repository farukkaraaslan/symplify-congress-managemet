using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Domain.Tenant;

public class Tenant : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? HostUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<TenantUser> TenantUsers { get; set; } = new HashSet<TenantUser>();
    public virtual ICollection<TenantApiKey> ApiKeys { get; set; } = new HashSet<TenantApiKey>();
    public virtual ICollection<Symplify.BackOffice.Domain.Congress.Congress> Congresses { get; set; } = new HashSet<Symplify.BackOffice.Domain.Congress.Congress>();
}
