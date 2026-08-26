namespace Symplify.BackOffice.Persistence.Seeding.Options;

public sealed class DatabaseInitializationOptions
{
    public const string SectionName = "DatabaseInitialization";

    /// <summary>
    /// Applies EF Core migrations on application startup.
    /// Production default must be false. Enable explicitly only during controlled deployment.
    /// </summary>
    public bool ApplyMigrations { get; set; } = false;

    /// <summary>
    /// Master switch for all seed operations.
    /// If false, no seed/backfill operation is executed.
    /// </summary>
    public bool RunSeed { get; set; } = false;

    /// <summary>
    /// Runs only production-safe core seed:
    /// languages, localization resources, identity roles/claims/admin and global workflow definitions.
    /// </summary>
    public bool RunCoreSeed { get; set; } = false;

    /// <summary>
    /// Runs demo/sample data seed.
    /// Must remain false in production.
    /// </summary>
    public bool RunDemoSeed { get; set; } = false;

    /// <summary>
    /// Backfills congress workflow settings/transitions for existing congresses.
    /// Enable only after congress data is ready and backfill is required.
    /// </summary>
    public bool RunCongressWorkflowBackfill { get; set; } = false;

    /// <summary>
    /// Legacy fallback for projects without migrations.
    /// Should remain false for this project in production.
    /// </summary>
    public bool UseEnsureCreatedWhenNoMigrations { get; set; } = false;
}
