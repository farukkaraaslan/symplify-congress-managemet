using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Symplify.BackOffice.WebUI.Models.Profile;

public sealed class ProfileViewModel
{
    public ProfileUpdateViewModel Profile { get; set; } = new();

    public ChangePasswordViewModel ChangePassword { get; set; } = new();

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}

public sealed class ProfileUpdateViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "BackOffice.Profile.Validation.NameRequired")]
    [StringLength(100, ErrorMessage = "BackOffice.Profile.Validation.NameMaxLength")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "BackOffice.Profile.Validation.SurnameRequired")]
    [StringLength(100, ErrorMessage = "BackOffice.Profile.Validation.SurnameMaxLength")]
    public string Surname { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumberDisplay { get; set; }

    [Required(ErrorMessage = "BackOffice.Profile.Validation.PhoneRequired")]
    [StringLength(32, ErrorMessage = "BackOffice.Profile.Validation.PhoneInvalid")]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "BackOffice.Profile.Validation.InstitutionMaxLength")]
    public string? Institution { get; set; }

    [StringLength(100, ErrorMessage = "BackOffice.Profile.Validation.OrcidMaxLength")]
    public string? Orcid { get; set; }

    public string? ProfileImageUrl { get; set; }

    public IFormFile? ProfileImageFile { get; set; }
}

public sealed class ChangePasswordViewModel
{
    [Required(ErrorMessage = "BackOffice.Profile.Validation.CurrentPasswordRequired")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "BackOffice.Profile.Validation.NewPasswordRequired")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "BackOffice.Profile.Validation.NewPasswordMinLength")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "BackOffice.Profile.Validation.ConfirmPasswordRequired")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "BackOffice.Profile.Validation.PasswordCompare")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class CompletePhoneViewModel
{
    public string? PhoneNumberDisplay { get; set; }

    [Required(ErrorMessage = "BackOffice.Profile.CompletePhone.Validation.PhoneRequired")]
    [StringLength(32, ErrorMessage = "BackOffice.Profile.CompletePhone.Validation.PhoneInvalid")]
    public string PhoneNumber { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
