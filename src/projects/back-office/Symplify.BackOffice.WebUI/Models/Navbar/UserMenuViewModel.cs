namespace Symplify.BackOffice.WebUI.Models.Navbar;

public sealed class UserMenuViewModel
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PrimaryRole { get; set; } = string.Empty;

    public string Initials { get; set; } = "U";

    public string? ProfileImageUrl { get; set; }

    public string ProfileUrl { get; set; } = string.Empty;

    public string SettingsUrl { get; set; } = string.Empty;

    public string LogoutUrl { get; set; } = string.Empty;
}
