namespace Symplify.BackOffice.Application.Features.Congresses.Queries.GetList;

public class GetListCongressListItemDto
{

    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public int? EditionNumber { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Symplify.BackOffice.Domain.Enums.CongressStatus Status { get; set; }
    public string? ContactName { get; set; }
    public string? ContactTitle { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactAddress { get; set; }
    public string? VenueName { get; set; }
    public Guid? CountryId { get; set; }
    public Guid? CityId { get; set; }
    public Guid? StateId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? WelcomeTitle { get; set; }
    public string? WelcomeContent { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? LogoLightPath { get; set; }
    public string? LogoDarkPath { get; set; }
    public string? LogoLightUrl { get; set; }
    public string? LogoDarkUrl { get; set; }

    // Backward-compatible display logo; points to light logo.
    public string? LogoPath { get; set; }
    public string? LogoUrl { get; set; }
    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
    public List<string> TranslationCultures { get; set; } = new();
}
