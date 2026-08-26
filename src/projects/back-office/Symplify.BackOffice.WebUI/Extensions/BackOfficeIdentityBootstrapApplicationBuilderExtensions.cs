using Symplify.BackOffice.WebUI.Services.Bootstrap;

namespace Symplify.BackOffice.WebUI.Extensions;

public static class BackOfficeIdentityBootstrapApplicationBuilderExtensions
{
    public static async Task InitializeBackOfficeIdentityAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        IBackOfficeIdentityBootstrapper bootstrapper =
            scope.ServiceProvider.GetRequiredService<IBackOfficeIdentityBootstrapper>();

        await bootstrapper.BootstrapAsync();
    }
}
