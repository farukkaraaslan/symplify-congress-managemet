namespace Symplify.BackOffice.WebUI.Models.LanguageSwitcher;


public sealed class LanguageSwitcherViewModel
{
    public string CurrentCulture { get; set; } = "tr-TR";

    public LanguageSwitcherItemViewModel? CurrentLanguage { get; set; }

    public IReadOnlyList<LanguageSwitcherItemViewModel> Languages { get; set; } =
        Array.Empty<LanguageSwitcherItemViewModel>();
}