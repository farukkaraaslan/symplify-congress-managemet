namespace Symplify.BackOffice.Application.Features.Organizations.Commands.Create;

public class CreatedOrganizationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortName { get; set; }
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
    public bool IsActive { get; set; }
}
