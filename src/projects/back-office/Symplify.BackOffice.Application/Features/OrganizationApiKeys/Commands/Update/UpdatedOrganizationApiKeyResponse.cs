namespace Symplify.BackOffice.Application.Features.OrganizationApiKeys.Commands.Update;

public class UpdatedOrganizationApiKeyResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string KeyType { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Scopes { get; set; } = string.Empty;
    public string? AllowedIpAddresses { get; set; }
    public string? AllowedDomains { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
}
