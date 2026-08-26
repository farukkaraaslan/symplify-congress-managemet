using Microsoft.Extensions.DependencyInjection;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Persistence.Repositories;

namespace Symplify.BackOffice.Persistence.DependencyInjection;

/// <summary>
/// Organization mail configuration persistence dependencies.
/// This registration belongs to the Persistence composition root,
/// not to WebUI/Program.cs.
/// </summary>
internal static class OrganizationMailPersistenceServiceRegistration
{
    public static IServiceCollection AddOrganizationMailPersistenceServices(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<
            IOrganizationMailConfigurationRepository,
            OrganizationMailConfigurationRepository>();

        return services;
    }
}
