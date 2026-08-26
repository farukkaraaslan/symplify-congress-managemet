using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.WebUI.Models.Congresses;

public sealed class ManageCongressViewModel
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string? Description { get; set; }

    public string? ShortDescription { get; set; }

    public string? WelcomeTitle { get; set; }

    public string? WelcomeContent { get; set; }

    public string? LogoLightPath { get; set; }

    public string? LogoDarkPath { get; set; }

    public string? LogoLightUrl { get; set; }

    public string? LogoDarkUrl { get; set; }

    public string? LogoPath { get; set; }

    public string? LogoUrl { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public CongressStatus Status { get; set; }

    public Guid DisplayLanguageId { get; set; }

    public bool IsFallback { get; set; }

    public List<string> TranslationCultures { get; set; } = new();

    public string ActiveTab { get; set; } = "congress";
}
