using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Domain.ShortLinks;

public sealed class ShortLink : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public string Code { get; set; } = string.Empty;

    public ShortLinkTargetType TargetType { get; set; }

    public Guid TargetId { get; set; }

    public string? Culture { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public int ClickCount { get; set; }

    public DateTime? LastAccessedAt { get; set; }

    public bool IsActive { get; set; } = true;
}
