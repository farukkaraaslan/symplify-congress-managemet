using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Organization;

public class Organization : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public string Name { get; set; } = null!;

    // UI/business code used by the Organization screens. Kept separate from Slug,
    // but Slug is also populated from Code for backward compatibility with the old Tenant model.
    public string Code { get; set; } = null!;

    public string Slug { get; set; } = null!;

    /// <summary>
    /// Corporate short code/name used as the stable prefix for congress code generation and object-storage file naming.
    /// Example: UTSAK, ISPEC, UBAK.
    /// </summary>
    public string ShortName { get; set; } = null!;

    public string? WebsiteUrl { get; set; }
    public string? HostUrl { get; set; }
    public string? Description { get; set; }

    public string? ContactName { get; set; }
    public string? ContactTitle { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactNote { get; set; }

    public string? LogoLightPath { get; set; }

    public string? LogoDarkPath { get; set; }

    public string? BrandColor { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual OrganizationMailConfiguration? MailConfiguration { get; set; }

    public virtual ICollection<OrganizationUser> OrganizationUsers { get; set; } = new HashSet<OrganizationUser>();
    public virtual ICollection<OrganizationApiKey> ApiKeys { get; set; } = new HashSet<OrganizationApiKey>();
    public virtual ICollection<Symplify.BackOffice.Domain.Congress.Congress> Congresses { get; set; } = new HashSet<Symplify.BackOffice.Domain.Congress.Congress>();
}
