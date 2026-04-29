using System.ComponentModel.DataAnnotations;

namespace Core.Persistence.Repositories;

public class Entity<TId> : IAuditable, IEntityTimestamps
{
    [Key]
    public TId Id { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? DeletedDate { get; set; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public string? DeletedBy { get; set; }

    public Entity()
    {
        Id = default!;
    }

    public Entity(TId id)
    {
        Id = id;
    }
}

