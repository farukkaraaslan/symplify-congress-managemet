using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Localization;

public class Language : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public string Name { get; set; } = null!;
    public string Culture { get; set; } = null!;
    public string? TwoLetterIsoCode { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public int Order { get; set; }

    public virtual ICollection<ResourceValue> ResourceValues { get; set; } = new HashSet<ResourceValue>();
}
