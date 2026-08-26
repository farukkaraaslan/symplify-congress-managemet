namespace Symplify.BackOffice.WebUI.Models.CongressSliders;

public sealed class CongressSliderTranslationViewModel
{
    public Guid LanguageId { get; set; }
    public string Culture { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool Exists { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? ButtonText { get; set; }
    public string? ButtonUrl { get; set; }
}
