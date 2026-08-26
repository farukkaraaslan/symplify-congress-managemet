using Core.Persistence.Repositories;
namespace Symplify.BackOffice.Domain.Organization;
public class OrganizationApiKey : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = null!;
    public string Environment { get; set; } = "Production"; // Production / Sandbox / Development
    public string KeyType { get; set; } = "SecretKey"; // SecretKey / PublicKey / IntegrationKey
    public string KeyPrefix { get; set; } = null!;
    public string KeyHash { get; set; } = null!;
    public string? Description { get; set; }
    public string Scopes { get; set; } = string.Empty; // comma-separated scope keys
    public string? AllowedIpAddresses { get; set; }
    public string? AllowedDomains { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public virtual Organization Organization { get; set; } = null!;
}
