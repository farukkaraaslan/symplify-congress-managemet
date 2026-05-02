namespace Symplify.BackOffice.Application.Features.Tenants.Commands.Create;
public class CreatedTenantResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? HostUrl { get; set; }
    public bool IsActive { get; set; }
}
