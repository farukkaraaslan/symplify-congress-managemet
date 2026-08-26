namespace Symplify.BackOffice.Application.Features.OrganizationApiKeys.Commands.Delete;

public class DeletedOrganizationApiKeyResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string KeyType { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
}
