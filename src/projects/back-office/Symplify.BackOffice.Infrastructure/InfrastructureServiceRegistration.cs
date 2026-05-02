using Microsoft.Extensions.DependencyInjection;
using Symplify.BackOffice.Application.Services.Authentication;
using Symplify.BackOffice.Infrastructure.Identity;

namespace Symplify.BackOffice.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddBackOfficeInfrastructureServices(
        this IServiceCollection services)
    {
        services.AddScoped<IBackOfficeIdentityService, BackOfficeIdentityService>();

        return services;
    }
}
