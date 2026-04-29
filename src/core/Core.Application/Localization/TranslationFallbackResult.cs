namespace Core.Application.Localization;

public sealed record TranslationFallbackResult<TTranslation>(
    TTranslation? Translation,
    bool IsFallback,
    string? MatchedCulture)
{
    public bool HasValue => Translation is not null;
}
