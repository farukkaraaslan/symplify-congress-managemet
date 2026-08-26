using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Localization;

public class ResourceKey : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public string KeyName { get; set; } = null!;
    public string? AreaName { get; set; }
    public string? Description { get; set; }

    public virtual ICollection<ResourceValue> Values { get; set; } = new HashSet<ResourceValue>();
}
