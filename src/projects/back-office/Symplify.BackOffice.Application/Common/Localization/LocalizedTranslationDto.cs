namespace Symplify.BackOffice.Application.Common.Localization;
public sealed class LocalizedTranslationDto
{
    public Guid LanguageId { get; set; }
    public string Culture { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool Exists { get; set; }
    public Dictionary<string, string?> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
