namespace Symplify.BackOffice.Persistence.Seeding.Options;

public sealed class DatabaseInitializationOptions
{
    public const string SectionName = "DatabaseInitialization";

    public bool ApplyMigrations { get; set; } = true;

    public bool RunSeed { get; set; } = true;

    public bool UseEnsureCreatedWhenNoMigrations { get; set; }
}
