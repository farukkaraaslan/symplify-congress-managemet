using Microsoft.Extensions.DependencyInjection;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Persistence.Repositories;

namespace Symplify.BackOffice.Persistence.DependencyInjection;

internal static class CongressContactEmailPersistenceServiceRegistration
{
    public static IServiceCollection AddCongressContactEmailPersistenceServices(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<
            ICongressContactEmailRepository,
            CongressContactEmailRepository>();

        return services;
    }
}
