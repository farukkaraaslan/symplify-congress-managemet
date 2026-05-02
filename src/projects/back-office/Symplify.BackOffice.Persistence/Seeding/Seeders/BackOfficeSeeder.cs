using Microsoft.Extensions.Logging;
using Symplify.BackOffice.Persistence.Seeding.Abstractions;

namespace Symplify.BackOffice.Persistence.Seeding.Seeders;

public sealed class BackOfficeSeeder : IBackOfficeSeeder
{
    private readonly LanguageSeeder _languageSeeder;
    private readonly LocalizationResourceSeeder _localizationResourceSeeder;
    private readonly ILogger<BackOfficeSeeder> _logger;

    public BackOfficeSeeder(
        LanguageSeeder languageSeeder,
        LocalizationResourceSeeder localizationResourceSeeder,
        ILogger<BackOfficeSeeder> logger)
    {
        _languageSeeder = languageSeeder;
        _localizationResourceSeeder = localizationResourceSeeder;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("BackOffice seed started.");

        await _languageSeeder.SeedAsync(cancellationToken);

        await _localizationResourceSeeder.SeedAsync(cancellationToken);

        _logger.LogInformation("BackOffice seed completed.");
    }
}