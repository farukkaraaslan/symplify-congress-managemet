using Microsoft.AspNetCore.Mvc.Rendering;

namespace Symplify.BackOffice.WebUI.Models.Auth;

public sealed class RegisterViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Institution { get; set; } = string.Empty;

    public Guid? TitleId { get; set; }

    public Guid? OrganizationId { get; set; }

    public string? OrganizationSlug { get; set; }

    public string? OrganizationName { get; set; }

    public string? OrganizationShortName { get; set; }

    public string? OrganizationLogoLightUrl { get; set; }

    public string? OrganizationLogoDarkUrl { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? StateId { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public string? PhoneCountryIso2 { get; set; }

    public string? PhoneDialCode { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string ConfirmPassword { get; set; } = string.Empty;

    public bool AcceptTerms { get; set; }

    public bool HasOrganizationContext => OrganizationId.HasValue && OrganizationId.Value != Guid.Empty;

    public List<SelectListItem> TitleOptions { get; set; } = new();

    public List<SelectListItem> CountryOptions { get; set; } = new();

    public List<SelectListItem> StateOptions { get; set; } = new();
}
