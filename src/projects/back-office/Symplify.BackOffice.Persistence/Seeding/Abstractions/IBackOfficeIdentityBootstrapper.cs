namespace Symplify.BackOffice.Persistence.Seeding.Abstractions;

public interface IBackOfficeIdentityBootstrapper
{
    Task BootstrapAsync(CancellationToken cancellationToken = default);
}
