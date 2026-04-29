namespace Core.Application.Localization;

public interface ITranslationFallbackResolver
{
    TranslationFallbackResult<TTranslation> Resolve<TTranslation>(
        IEnumerable<TTranslation>? translations,
        string requestedCulture,
        string defaultCulture,
        Func<TTranslation, string?> cultureSelector,
        Func<TTranslation, bool> isUsable);
}
