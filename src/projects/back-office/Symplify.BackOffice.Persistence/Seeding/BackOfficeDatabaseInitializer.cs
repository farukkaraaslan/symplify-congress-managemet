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
        if (_options.ApplyMigrations)
        {
            _logger.LogInformation("Applying database migrations.");
            await _context.Database.MigrateAsync(cancellationToken);
        }

        if (_options.RunSeed)
        {
            _logger.LogInformation("Running BackOffice seed.");
            await _seeder.SeedAsync(cancellationToken);
        }
    }
}