using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Services.Localization;

namespace Symplify.BackOffice.WebUI.Extensions;

public static class BackOfficeViewLocalizationServiceCollectionExtensions
{
    public static IServiceCollection AddBackOfficeDbViewLocalization(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<IBackOfficeCultureResolver, BackOfficeCultureResolver>();
        services.AddScoped<IBackOfficeViewLocalizer, DbResourceViewLocalizer>();

        return services;
    }
}