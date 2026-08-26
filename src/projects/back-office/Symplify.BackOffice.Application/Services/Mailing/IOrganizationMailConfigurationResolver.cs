namespace Symplify.BackOffice.Application.Services.Mailing;

public interface IOrganizationMailConfigurationResolver
{
    Task<ResolvedOrganizationMailConfiguration> ResolveAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
