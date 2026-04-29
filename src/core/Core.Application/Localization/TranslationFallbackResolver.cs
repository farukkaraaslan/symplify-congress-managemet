namespace Core.Application.Localization;

public sealed class TranslationFallbackResolver : ITranslationFallbackResolver
{
    public TranslationFallbackResult<TTranslation> Resolve<TTranslation>(
        IEnumerable<TTranslation>? translations,
        string requestedCulture,
        string defaultCulture,
        Func<TTranslation, string?> cultureSelector,
        Func<TTranslation, bool> isUsable)
    {
        var source = translations?
            .Where(x => x is not null)
            .Where(isUsable)
            .ToList() ?? new List<TTranslation>();

        if (source.Count == 0)
            return new TranslationFallbackResult<TTranslation>(default, true, null);

        var exact = source.FirstOrDefault(x =>
            CultureEquals(cultureSelector(x), requestedCulture));

        if (exact is not null)
        {
            return new TranslationFallbackResult<TTranslation>(
                exact,
                false,
                cultureSelector(exact));
        }

        var baseCulture = GetBaseCulture(requestedCulture);

        if (!string.IsNullOrWhiteSpace(baseCulture))
        {
            var baseMatch = source.FirstOrDefault(x =>
            {
                var culture = cultureSelector(x);

                return CultureEquals(culture, baseCulture)
                       || StartsWithBaseCulture(culture, baseCulture);
            });

            if (baseMatch is not null)
            {
                return new TranslationFallbackResult<TTranslation>(
                    baseMatch,
                    true,
                    cultureSelector(baseMatch));
            }
        }

        var defaultMatch = source.FirstOrDefault(x =>
            CultureEquals(cultureSelector(x), defaultCulture));

        if (defaultMatch is not null)
        {
            return new TranslationFallbackResult<TTranslation>(
                defaultMatch,
                true,
                cultureSelector(defaultMatch));
        }

        var any = source.FirstOrDefault();

        return new TranslationFallbackResult<TTranslation>(
            any,
            true,
            any is null ? null : cultureSelector(any));
    }

    private static bool CultureEquals(string? left, string? right)
    {
        return !string.IsNullOrWhiteSpace(left)
               && !string.IsNullOrWhiteSpace(right)
               && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static bool StartsWithBaseCulture(string? culture, string baseCulture)
    {
        return !string.IsNullOrWhiteSpace(culture)
               && culture.StartsWith(baseCulture + "-", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetBaseCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return null;

        return culture
            .Split('-', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
    }
}
