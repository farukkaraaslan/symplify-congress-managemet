using Microsoft.AspNetCore.Mvc.Rendering;

namespace Symplify.BackOffice.WebUI.Models.OrganizationMailConfigurations;

public sealed class OrganizationMailConfigurationsIndexViewModel
{
    public Guid? SelectedOrganizationId { get; set; }
    public string? SelectedOrganizationName { get; set; }
    public string? SelectedOrganizationCode { get; set; }
    public IReadOnlyList<SelectListItem> Organizations { get; set; } = Array.Empty<SelectListItem>();

    public OrganizationMailConfigurationViewModel Configuration { get; set; } = new();
    public bool Exists { get; set; }
    public bool HasStoredPassword { get; set; }
    public bool HasMailLogo { get; set; }
    public string? MailLogoFileName { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public bool? LastTestSucceeded { get; set; }
    public string? LastTestError { get; set; }
}
