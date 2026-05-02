using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressTopic : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }
    public Guid TopicId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Congress Congress { get; set; } = null!;
    public virtual Topic Topic { get; set; } = null!;
}
