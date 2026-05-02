using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Tenant;

public class TenantApiKey : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string KeyPrefix { get; set; } = null!;
    public string KeyHash { get; set; } = null!;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Tenant Tenant { get; set; } = null!;
}
