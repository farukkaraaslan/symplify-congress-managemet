namespace Symplify.BackOffice.Persistence.Seeding.Abstractions;

public interface IBackOfficeSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
