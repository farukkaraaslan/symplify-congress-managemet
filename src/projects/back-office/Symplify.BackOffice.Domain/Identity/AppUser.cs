using Core.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Domain.Tenant;

namespace Symplify.BackOffice.Domain.Identity;

public class AppUser : IdentityUser<Guid>, IEntityTimestamps, IAuditable
{
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public Guid? TitleId { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? Institution { get; set; }
    public string? Orcid { get; set; }

    public virtual Title? Title { get; set; }
    public virtual ICollection<TenantUser> TenantUsers { get; set; } = new HashSet<TenantUser>();
    public virtual ICollection<Reviewer> Reviewers { get; set; } = new HashSet<Reviewer>();
    public virtual ICollection<Symplify.BackOffice.Domain.Submission.Submission> Submissions { get; set; } = new HashSet<Symplify.BackOffice.Domain.Submission.Submission>();
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? DeletedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public string? DeletedBy { get; set; }
}
