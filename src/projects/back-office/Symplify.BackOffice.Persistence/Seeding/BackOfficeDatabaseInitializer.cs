using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Persistence.Contexts;
using Symplify.BackOffice.Persistence.Seeding.Abstractions;
using Symplify.BackOffice.Persistence.Seeding.Options;

namespace Symplify.BackOffice.Persistence.Seeding.Seeders;

public sealed class BackOfficeDatabaseInitializer : IBackOfficeDatabaseInitializer
{
    private readonly BackOfficeDbContext _context;
    private readonly IBackOfficeSeeder _seeder;
    private readonly DatabaseInitializationOptions _options;
    private readonly ILogger<BackOfficeDatabaseInitializer> _logger;

    public BackOfficeDatabaseInitializer(
        BackOfficeDbContext context,
        IBackOfficeSeeder seeder,
        IOptions<DatabaseInitializationOptions> options,
        ILogger<BackOfficeDatabaseInitializer> logger)
    {
        _context = context;
        _seeder = seeder;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Database initialization options. ApplyMigrations: {ApplyMigrations}, RunSeed: {RunSeed}, RunCoreSeed: {RunCoreSeed}, RunDemoSeed: {RunDemoSeed}, WorkflowBackfill: {WorkflowBackfill}, EnsureCreated: {EnsureCreated}",
            _options.ApplyMigrations,
            _options.RunSeed,
            _options.RunCoreSeed,
            _options.RunDemoSeed,
            _options.RunCongressWorkflowBackfill,
            _options.UseEnsureCreatedWhenNoMigrations);

        if (!_options.ApplyMigrations &&
            !_options.RunSeed &&
            !_options.UseEnsureCreatedWhenNoMigrations)
        {
            _logger.LogInformation("Database initialization skipped.");
            return;
        }

        if (_options.ApplyMigrations)
        {
            _logger.LogInformation("Applying database migrations.");

            IEnumerable<string> pendingMigrations = await _context.Database
                .GetPendingMigrationsAsync(cancellationToken);

            if (pendingMigrations.Any())
            {
                await _context.Database.MigrateAsync(cancellationToken);
            }
            else
            {
                _logger.LogInformation("No migrations were applied. The database is already up to date.");
            }
        }
        else if (_options.UseEnsureCreatedWhenNoMigrations)
        {
            IReadOnlyList<string> migrations = _context.Database
       .GetMigrations()
       .ToList();

            if (migrations.Count == 0)
            {
                _logger.LogInformation("No EF Core migrations found. Running EnsureCreated.");
                await _context.Database.EnsureCreatedAsync(cancellationToken);
            }
            else
            {
                _logger.LogWarning(
                    "UseEnsureCreatedWhenNoMigrations is true, but migrations exist. EnsureCreated was skipped.");
            }
        }

        if (_options.RunSeed)
        {
            _logger.LogInformation("Running BackOffice seed.");
            await _seeder.SeedAsync(cancellationToken);
        }
        else
        {
            _logger.LogInformation("BackOffice seed skipped.");
        }
    }
}
