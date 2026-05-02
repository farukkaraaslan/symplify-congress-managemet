using System.Reflection;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Pipelines.Logging;
using Core.Application.Pipelines.Transaction;
using Core.Application.Pipelines.Validation;
using Core.Application.Rules;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Symplify.BackOffice.Application.Services.Localization;

namespace Symplify.BackOffice.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddBackOfficeApplicationServices(this IServiceCollection services)
    {
        Assembly assembly = typeof(ApplicationServiceRegistration).Assembly;

        services.AddAutoMapper(config =>
        {
            config.AddMaps(assembly);
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);

            configuration.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
            configuration.AddOpenBehavior(typeof(CachingBehavior<,>));
            configuration.AddOpenBehavior(typeof(CacheRemovingBehavior<,>));
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(RequestValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(TransactionScopeBehavior<,>));
        });

        services.AddSubClassesOfType(assembly, typeof(BaseBusinessRules));

        services.AddScoped<IApplicationLanguageProvider, ApplicationLanguageProvider>();
        services.AddScoped<ICurrentLanguageProvider, CurrentLanguageProvider>();
        services.AddScoped<ITranslationFallbackResolver, TranslationFallbackResolver>();

        return services;
    }

    public static IServiceCollection AddSubClassesOfType(
        this IServiceCollection services,
        Assembly assembly,
        Type type,
        Func<IServiceCollection, Type, IServiceCollection>? addWithLifeCycle = null)
    {
        List<Type> types = assembly
            .GetTypes()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                t.IsSubclassOf(type) &&
                t != type)
            .ToList();

        foreach (Type item in types)
        {
            if (addWithLifeCycle is null)
            {
                services.AddScoped(item);
                continue;
            }

            addWithLifeCycle(services, item);
        }

        return services;
    }
}