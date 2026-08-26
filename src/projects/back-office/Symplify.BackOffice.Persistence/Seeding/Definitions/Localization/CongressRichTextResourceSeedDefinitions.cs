namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class CongressRichTextResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new(
            "BackOffice.Congresses",
            "BackOffice.Congresses.Validation.TranslationFieldMaxLengthExceeded",
            "Kongre çeviri alanlarında izin verilen karakter sınırı aşıldı. Karşılama yazısı gibi uzun içeriklerde en fazla 20.000 karakter kullanılabilir.",
            "The allowed character limit was exceeded in congress translation fields. Long content such as welcome text can contain at most 20,000 characters.")
    };
}
