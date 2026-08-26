using Microsoft.Extensions.DependencyInjection;

namespace Symplify.Api.Application.DependencyInjection;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddSymplifyApiApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(ApplicationServiceRegistration).Assembly));

        return services;
    }
}
