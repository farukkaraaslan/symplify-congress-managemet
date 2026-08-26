using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Symplify.BackOffice.WebUI.Models.Organizations;

public class CreateOrganizationViewModel
{
    [Required(ErrorMessage = "BackOffice.Organizations.Validation.NameRequired")]
    [MaxLength(200, ErrorMessage = "BackOffice.Organizations.Validation.NameMaxLength")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "BackOffice.Organizations.Validation.CodeRequired")]
    [MaxLength(80, ErrorMessage = "BackOffice.Organizations.Validation.CodeMaxLength")]
    [RegularExpression("^[a-zA-Z0-9-]+$", ErrorMessage = "BackOffice.Organizations.Validation.InvalidCode")]
    public string? Code { get; set; }

    // Backward-compatible alias for old Tenant modal/views.
    // Old UI used Slug; new Organization UI uses Code.
    [MaxLength(80, ErrorMessage = "BackOffice.Organizations.Validation.CodeMaxLength")]
    [RegularExpression("^[a-zA-Z0-9-]+$", ErrorMessage = "BackOffice.Organizations.Validation.InvalidCode")]
    public string? Slug
    {
        get => Code;
        set => Code = value;
    }

    [Required(ErrorMessage = "BackOffice.Organizations.Validation.ShortNameRequired")]
    [MaxLength(80, ErrorMessage = "BackOffice.Organizations.Validation.ShortNameMaxLength")]
    [RegularExpression("^[a-zA-Z0-9-]+$", ErrorMessage = "BackOffice.Organizations.Validation.InvalidShortName")]
    public string? ShortName { get; set; }

    [MaxLength(500, ErrorMessage = "BackOffice.Organizations.Validation.WebsiteUrlMaxLength")]
    [Url(ErrorMessage = "BackOffice.Organizations.Validation.InvalidWebsiteUrl")]
    public string? WebsiteUrl { get; set; }

    // Backward-compatible alias for old Tenant modal/views.
    // Old UI used HostUrl; new Organization UI uses WebsiteUrl.
    [MaxLength(500, ErrorMessage = "BackOffice.Organizations.Validation.HostUrlMaxLength")]
    [Url(ErrorMessage = "BackOffice.Organizations.Validation.InvalidWebsiteUrl")]
    public string? HostUrl
    {
        get => WebsiteUrl;
        set => WebsiteUrl = value;
    }

    [MaxLength(1000, ErrorMessage = "BackOffice.Organizations.Validation.DescriptionMaxLength")]
    public string? Description { get; set; }

    [MaxLength(200, ErrorMessage = "BackOffice.Organizations.Validation.ContactNameMaxLength")]
    public string? ContactName { get; set; }

    [MaxLength(200, ErrorMessage = "BackOffice.Organizations.Validation.ContactTitleMaxLength")]
    public string? ContactTitle { get; set; }

    [EmailAddress(ErrorMessage = "BackOffice.Organizations.Validation.InvalidContactEmail")]
    [MaxLength(256, ErrorMessage = "BackOffice.Organizations.Validation.ContactEmailMaxLength")]
    public string? ContactEmail { get; set; }

    [MaxLength(50, ErrorMessage = "BackOffice.Organizations.Validation.ContactPhoneMaxLength")]
    public string? ContactPhone { get; set; }

    [MaxLength(1000, ErrorMessage = "BackOffice.Organizations.Validation.ContactNoteMaxLength")]
    public string? ContactNote { get; set; }

    [MaxLength(20, ErrorMessage = "BackOffice.Organizations.Validation.BrandColorMaxLength")]
    public string? BrandColor { get; set; } = "#487FFF";

    public IFormFile? LogoLightFile { get; set; }

    public IFormFile? LogoDarkFile { get; set; }

    // Backward-compatible alias for old modal/views. New screens use light/dark logo files.
    public IFormFile? LogoFile
    {
        get => LogoLightFile;
        set => LogoLightFile = value;
    }

    public bool IsActive { get; set; } = true;
}
