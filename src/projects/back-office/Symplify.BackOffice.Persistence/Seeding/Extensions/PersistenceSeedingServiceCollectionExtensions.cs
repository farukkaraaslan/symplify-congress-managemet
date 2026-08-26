using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Symplify.BackOffice.Persistence.Seeding.Abstractions;
using Symplify.BackOffice.Persistence.Seeding.Options;
using Symplify.BackOffice.Persistence.Seeding.Seeders;

namespace Symplify.BackOffice.Persistence.Seeding.Extensions;

public static class PersistenceSeedingServiceCollectionExtensions
{
    public static IServiceCollection AddBackOfficePersistenceSeedingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<DatabaseInitializationOptions>(
            configuration.GetSection(DatabaseInitializationOptions.SectionName));

        services.Configure<LocalizationSeedOptions>(
            configuration.GetSection(LocalizationSeedOptions.SectionName));

        services.AddScoped<IBackOfficeDatabaseInitializer, BackOfficeDatabaseInitializer>();
        services.AddScoped<IBackOfficeSeeder, BackOfficeSeeder>();

        services.AddScoped<LanguageSeeder>();
        services.AddScoped<LocalizationResourceSeeder>();
        services.AddScoped<LocalizationSeedValidator>();
        services.AddScoped<BackOfficeDemoDataSeeder>();
        services.AddScoped<CongressWorkflowBackfillSeeder>();

        services.AddScoped<IBackOfficeIdentityBootstrapper, BackOfficeIdentityBootstrapper>();

        return services;
    }
}
