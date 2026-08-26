namespace Symplify.BackOffice.Application.Features.OrganizationApiKeys.Commands.Create;

public class CreatedOrganizationApiKeyResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string KeyType { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string PlainTextKey { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}
