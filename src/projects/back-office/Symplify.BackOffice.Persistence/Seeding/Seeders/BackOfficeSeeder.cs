using Microsoft.Extensions.Logging;
using Symplify.BackOffice.Persistence.Seeding.Abstractions;

namespace Symplify.BackOffice.Persistence.Seeding.Seeders;

public sealed class BackOfficeSeeder : IBackOfficeSeeder
{
    private readonly LanguageSeeder _languageSeeder;
    private readonly LocalizationResourceSeeder _localizationResourceSeeder;
    private readonly IBackOfficeIdentityBootstrapper _identityBootstrapper;
    private readonly ILogger<BackOfficeSeeder> _logger;

    public BackOfficeSeeder(
        LanguageSeeder languageSeeder,
        LocalizationResourceSeeder localizationResourceSeeder,
        IBackOfficeIdentityBootstrapper identityBootstrapper,
        ILogger<BackOfficeSeeder> logger)
    {
        _languageSeeder = languageSeeder;
        _localizationResourceSeeder = localizationResourceSeeder;
        _identityBootstrapper = identityBootstrapper;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("BackOffice seed started.");

        await _languageSeeder.SeedAsync(cancellationToken);

        await _localizationResourceSeeder.SeedAsync(cancellationToken);

        await _identityBootstrapper.BootstrapAsync(cancellationToken);

        _logger.LogInformation("BackOffice seed completed.");
    }
}