namespace Symplify.BackOffice.WebUI.Models.Navbar;

public sealed class NavbarCongressSelectorViewModel
{
    public Guid? SelectedCongressId { get; set; }

    public string CurrentCulture { get; set; } = "tr-TR";

    public string ClearUrl { get; set; } = string.Empty;

    public IReadOnlyList<NavbarCongressSelectorItemViewModel> Items { get; set; } =
        Array.Empty<NavbarCongressSelectorItemViewModel>();
}
