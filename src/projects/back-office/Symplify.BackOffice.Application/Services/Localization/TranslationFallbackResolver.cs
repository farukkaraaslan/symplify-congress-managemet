using Symplify.BackOffice.Application.Common.Localization;
namespace Symplify.BackOffice.Application.Services.Localization;
public sealed class TranslationFallbackResolver : ITranslationFallbackResolver
{
    public TTranslation? Resolve<TTranslation>(IEnumerable<TTranslation> translations, Guid requestedLanguageId, Guid defaultLanguageId) where TTranslation : class
    {
        List<TTranslation> items = translations.ToList();
        return items.FirstOrDefault(x => LocalizedEntityRuntimeHelper.GetPropertyValue(x, "LanguageId") is Guid id && id == requestedLanguageId)
            ?? items.FirstOrDefault(x => LocalizedEntityRuntimeHelper.GetPropertyValue(x, "LanguageId") is Guid id && id == defaultLanguageId);
    }
}
