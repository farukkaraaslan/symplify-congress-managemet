namespace Core.Application.Localization;

public interface ITranslationCompletenessService
{
    TranslationCompletenessResult Evaluate(
        IEnumerable<Guid> presentLanguageIds,
        IEnumerable<ApplicationLanguageInfo> activeLanguages);
}
