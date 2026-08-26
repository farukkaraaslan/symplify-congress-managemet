namespace Symplify.BackOffice.Application.Features.Organizations.Queries.GetList;

public class GetListOrganizationListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? HostUrl { get; set; }
    public string? LogoLightPath { get; set; }
    public string? LogoDarkPath { get; set; }
    public string? LogoLightUrl { get; set; }
    public string? LogoDarkUrl { get; set; }
    public string? BrandColor { get; set; }
    public bool IsActive { get; set; }
    public int ActiveApiKeyCount { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
