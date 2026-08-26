namespace Symplify.BackOffice.WebUI.Models.CongressAnnouncements;

public sealed class CongressAnnouncementTranslationViewModel
{
    public Guid LanguageId { get; set; }

    public string Culture { get; set; } = string.Empty;

    public string LanguageName { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool Exists { get; set; }

    public string? Title { get; set; }

    public string? Summary { get; set; }

    public string? Content { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }
}
