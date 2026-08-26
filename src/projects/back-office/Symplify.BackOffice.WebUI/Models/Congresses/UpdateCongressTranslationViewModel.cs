namespace Symplify.BackOffice.WebUI.Models.Congresses;

public sealed class UpdateCongressTranslationViewModel
{
    public Guid LanguageId { get; set; }

    public string Culture { get; set; } = string.Empty;

    public string LanguageName { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool Exists { get; set; }

    public string? Title { get; set; }

    /// <summary>
    /// Dar seçim/listelerde ve SEO fallback senaryolarında kullanılan kısa kongre başlığı.
    /// Veritabanında geriye uyumluluk için CongressTranslation.Subtitle alanında saklanır.
    /// </summary>
    public string? ShortTitle { get; set; }

    public string? WelcomeContent { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }
}
