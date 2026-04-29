namespace Core.Application.Localization;

public sealed record TranslationCompletenessResult(
    bool HasMissingTranslations,
    IReadOnlyList<string> MissingCultures)
{
    public string? MissingLanguagesText =>
        HasMissingTranslations
            ? string.Join(", ", MissingCultures)
            : null;
}
