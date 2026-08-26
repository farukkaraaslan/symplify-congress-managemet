namespace Symplify.BackOffice.Application.Services.Localization;
public interface ITranslationFallbackResolver
{
    TTranslation? Resolve<TTranslation>(IEnumerable<TTranslation> translations, Guid requestedLanguageId, Guid defaultLanguageId) where TTranslation : class;
}
