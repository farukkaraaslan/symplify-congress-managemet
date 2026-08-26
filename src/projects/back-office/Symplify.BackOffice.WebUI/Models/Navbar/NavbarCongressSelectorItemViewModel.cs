namespace Symplify.BackOffice.WebUI.Models.Navbar;

public sealed class NavbarCongressSelectorItemViewModel
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public bool IsSelected { get; set; }
}
