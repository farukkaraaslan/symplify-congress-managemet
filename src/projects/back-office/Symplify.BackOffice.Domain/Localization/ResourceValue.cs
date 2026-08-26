using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Localization;

public class ResourceValue : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid ResourceKeyId { get; set; }
    public Guid LanguageId { get; set; }
    public string Value { get; set; } = null!;

    public virtual ResourceKey ResourceKey { get; set; } = null!;
    public virtual Language Language { get; set; } = null!;
}
