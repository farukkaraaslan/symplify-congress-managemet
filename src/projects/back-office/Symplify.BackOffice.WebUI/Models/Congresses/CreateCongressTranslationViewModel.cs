namespace Symplify.BackOffice.WebUI.Models.Congresses;

public sealed class CreateCongressTranslationViewModel
{
    public Guid LanguageId { get; set; }

    public string Culture { get; set; } = string.Empty;

    public string LanguageName { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public string? Title { get; set; }

    /// <summary>
    /// Portal dışındaki dar seçim alanları, kısa listeler ve SEO fallback senaryoları için kullanılır.
    /// Veritabanında geriye uyumluluk amacıyla mevcut Subtitle alanına yazılır.
    /// Portal ana kongre kartında uzun Title gösterilmeye devam eder.
    /// </summary>
    public string? ShortTitle { get; set; }

    public string? WelcomeContent { get; set; }

    public string? SeoTitle { get; set; }

    public string? SeoDescription { get; set; }
}
