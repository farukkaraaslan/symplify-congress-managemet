using Core.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Symplify.BackOffice.Domain.Identity;

public class AppRole : IdentityRole<Guid>, IAuditable, IEntityTimestamps
{
    public string? Description { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public string? DeletedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public DateTime? DeletedDate { get; set; }

    public AppRole()
    {
        Id = Guid.NewGuid();
        CreatedDate = DateTime.UtcNow;
    }

    public AppRole(string roleName) : base(roleName)
    {
        Id = Guid.NewGuid();
        CreatedDate = DateTime.UtcNow;
    }
}