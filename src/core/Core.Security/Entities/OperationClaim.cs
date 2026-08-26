using Core.Persistence.Repositories;

namespace Core.Security.Entities;

public class OperationClaim : Entity<int>
{
    public required string Name { get; set; }

    public OperationClaim() { }

    public OperationClaim(int id, string name)
        : base(id)
    {
        Name = name;
    }
}
