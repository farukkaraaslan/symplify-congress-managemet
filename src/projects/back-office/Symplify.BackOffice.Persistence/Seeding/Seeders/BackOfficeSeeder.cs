using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Persistence.Seeding.Abstractions;
using Symplify.BackOffice.Persistence.Seeding.Options;

namespace Symplify.BackOffice.Persistence.Seeding.Seeders;

public sealed class BackOfficeSeeder : IBackOfficeSeeder
{
    private readonly LanguageSeeder _languageSeeder;
    private readonly LocalizationResourceSeeder _localizationResourceSeeder;
    private readonly LocalizationSeedValidator _localizationSeedValidator;
    private readonly IBackOfficeIdentityBootstrapper _identityBootstrapper;
    private readonly BackOfficeDemoDataSeeder _demoDataSeeder;
    private readonly CongressWorkflowBackfillSeeder _congressWorkflowBackfillSeeder;
    private readonly DatabaseInitializationOptions _dbOptions;
    private readonly LocalizationSeedOptions _localizationOptions;
    private readonly ILogger<BackOfficeSeeder> _logger;

    public BackOfficeSeeder(
        LanguageSeeder languageSeeder,
        LocalizationResourceSeeder localizationResourceSeeder,
        LocalizationSeedValidator localizationSeedValidator,
        IBackOfficeIdentityBootstrapper identityBootstrapper,
        BackOfficeDemoDataSeeder demoDataSeeder,
        CongressWorkflowBackfillSeeder congressWorkflowBackfillSeeder,
        IOptions<DatabaseInitializationOptions> dbOptions,
        IOptions<LocalizationSeedOptions> localizationOptions,
        ILogger<BackOfficeSeeder> logger)
    {
        _languageSeeder = languageSeeder;
        _localizationResourceSeeder = localizationResourceSeeder;
        _localizationSeedValidator = localizationSeedValidator;
        _identityBootstrapper = identityBootstrapper;
        _demoDataSeeder = demoDataSeeder;
        _congressWorkflowBackfillSeeder = congressWorkflowBackfillSeeder;
        _dbOptions = dbOptions.Value;
        _localizationOptions = localizationOptions.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "BackOffice seed started. CoreSeed: {CoreSeed}, DemoSeed: {DemoSeed}, WorkflowBackfill: {WorkflowBackfill}",
            _dbOptions.RunCoreSeed,
            _dbOptions.RunDemoSeed,
            _dbOptions.RunCongressWorkflowBackfill);

        if (_dbOptions.RunCoreSeed)
        {
            await _languageSeeder.SeedAsync(cancellationToken);

            await _localizationResourceSeeder.SeedAsync(cancellationToken);

            // Validate localization state after seeding when the seed is enabled.
            if (_localizationOptions.Enabled && _localizationOptions.ValidateAfterSeed)
            {
                await RunLocalizationValidationAsync(cancellationToken);
            }

            await _identityBootstrapper.BootstrapAsync(cancellationToken);
        }
        else
        {
            _logger.LogInformation("Core seed skipped.");
        }

        if (_dbOptions.RunDemoSeed)
        {
            await _demoDataSeeder.SeedAsync(cancellationToken);
        }
        else
        {
            _logger.LogInformation("Demo seed skipped.");
        }

        if (_dbOptions.RunCongressWorkflowBackfill)
        {
            await _congressWorkflowBackfillSeeder.SeedAsync(cancellationToken);
        }
        else
        {
            _logger.LogInformation("Congress workflow backfill skipped.");
        }

        _logger.LogInformation("BackOffice seed completed.");
    }

    private async Task RunLocalizationValidationAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running post-seed localization validation.");

        LocalizationValidationResult validationResult = await _localizationSeedValidator.ValidateAsync(
            LocalizationResourceSeeder.GetResources(),
            cancellationToken);

        if (!validationResult.IsValid && _localizationOptions.FailOnMissingTranslation)
        {
            int issueCount =
                validationResult.MissingDbKeys.Count +
                validationResult.MissingTurkishTranslations.Count +
                validationResult.MissingEnglishTranslations.Count +
                validationResult.EmptyTurkishInSeed.Count +
                validationResult.EmptyEnglishInSeed.Count;

            throw new InvalidOperationException(
                $"Localization seed validation failed with {issueCount} issue(s). " +
                $"Missing DB keys: {validationResult.MissingDbKeys.Count}, " +
                $"Missing tr-TR: {validationResult.MissingTurkishTranslations.Count}, " +
                $"Missing en-US: {validationResult.MissingEnglishTranslations.Count}. " +
                "Set LocalizationSeed:FailOnMissingTranslation to false to allow startup with missing translations.");
        }

        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Localization validation completed with issues. " +
                "FailOnMissingTranslation is false — application will continue.");
        }
    }
}
