namespace Symplify.BackOffice.WebUI.Services.Bootstrap;

public interface IBackOfficeIdentityBootstrapper
{
    Task BootstrapAsync(CancellationToken cancellationToken = default);
}
