namespace Symplify.BackOffice.Persistence.Seeding.Abstractions;

public interface IBackOfficeDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
