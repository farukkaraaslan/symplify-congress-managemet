using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressContactEmail : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }

    public string Email { get; set; } = null!;

    public string? Label { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsVisibleOnPortal { get; set; } = true;

    public bool ReceivesContactMessages { get; set; } = true;

    public int Order { get; set; }

    public virtual Congress Congress { get; set; } = null!;
}
