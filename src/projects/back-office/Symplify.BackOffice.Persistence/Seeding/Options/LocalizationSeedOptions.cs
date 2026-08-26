namespace Symplify.BackOffice.Persistence.Seeding.Options;

public sealed class LocalizationSeedOptions
{
    public const string SectionName = "LocalizationSeed";

    /// <summary>
    /// Master switch for the localization seed.
    /// When false, no localization seed or validation DB operation is executed,
    /// even if RunCoreSeed is true. Default is false to prevent unintended runs in production.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// When true, existing non-empty translation values are overwritten with the values
    /// defined in seed definitions. When false (default), only missing translations are inserted;
    /// user-modified translations are preserved.
    /// </summary>
    public bool OverwriteExistingTranslations { get; set; } = false;

    /// <summary>
    /// When true, a validation pass runs after seeding to verify that every seeded key
    /// and both tr-TR / en-US translations are present in the database.
    /// </summary>
    public bool ValidateAfterSeed { get; set; } = true;

    /// <summary>
    /// When true and the post-seed validation finds missing keys or translations,
    /// an exception is thrown and the application startup is aborted.
    /// When false, issues are logged as warnings but startup continues.
    /// </summary>
    public bool FailOnMissingTranslation { get; set; } = true;
}
