namespace Core.Application.Localization;

public sealed class TranslationCompletenessService : ITranslationCompletenessService
{
    public TranslationCompletenessResult Evaluate(
        IEnumerable<Guid> presentLanguageIds,
        IEnumerable<ApplicationLanguageInfo> activeLanguages)
    {
        var present = presentLanguageIds
            .Where(x => x != Guid.Empty)
            .ToHashSet();

        var missing = activeLanguages
            .Where(x => x.IsActive)
            .Where(x => !present.Contains(x.Id))
            .Where(x => !string.IsNullOrWhiteSpace(x.Culture))
            .Select(x => x.Culture)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        return new TranslationCompletenessResult(
            missing.Count > 0,
            missing);
    }
}
