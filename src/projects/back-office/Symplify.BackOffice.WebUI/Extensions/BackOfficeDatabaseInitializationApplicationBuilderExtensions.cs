using Symplify.BackOffice.Persistence.Seeding.Abstractions;

namespace Symplify.BackOffice.WebUI.Extensions;

public static class BackOfficeDatabaseInitializationApplicationBuilderExtensions
{
    public static async Task InitializeBackOfficeDatabaseAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        IBackOfficeDatabaseInitializer databaseInitializer = scope.ServiceProvider
            .GetRequiredService<IBackOfficeDatabaseInitializer>();

        await databaseInitializer.InitializeAsync();
}
}
