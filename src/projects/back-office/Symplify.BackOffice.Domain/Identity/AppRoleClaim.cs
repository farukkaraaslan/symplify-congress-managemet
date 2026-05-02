using Core.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;

namespace Symplify.BackOffice.Domain.Identity;

public class AppRoleClaim : IdentityRoleClaim<Guid>, IEntityTimestamps, IAuditable
{
    public string? DisplayName { get; set; }
    public string? Module { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get ; set ; }
    public DateTime? UpdatedDate { get ; set ; }
    public DateTime? DeletedDate { get ; set ; }
    public string? CreatedBy { get ; set ; }
    public string? UpdatedBy { get ; set ; }
    public string? DeletedBy { get ; set ; }
}
