namespace Symplify.BackOffice.WebUI.Models.LanguageSwitcher;

public sealed class LanguageSwitcherItemViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Culture { get; set; } = string.Empty;

    public string TwoLetterIsoCode { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool IsCurrent { get; set; }

    public string Url { get; set; } = string.Empty;
}
