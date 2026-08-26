using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Rules;

public sealed class OrganizationMailConfigurationBusinessRules : BaseBusinessRules
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IOrganizationMailConfigurationRepository _configurationRepository;

    public OrganizationMailConfigurationBusinessRules(
        IOrganizationRepository organizationRepository,
        IOrganizationMailConfigurationRepository configurationRepository)
    {
        _organizationRepository = organizationRepository;
        _configurationRepository = configurationRepository;
    }

    public async Task OrganizationShouldExistAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        Organization? organization = await _organizationRepository.GetAsync(
            predicate: item => item.Id == organizationId && item.DeletedDate == null,
            cancellationToken: cancellationToken);

        if (organization is null)
            throw new BusinessException(OrganizationMailConfigurationsMessages.OrganizationNotFound);
    }

    public async Task<OrganizationMailConfiguration> ConfigurationShouldExistAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        OrganizationMailConfiguration? configuration = await _configurationRepository.GetAsync(
            predicate: item => item.OrganizationId == organizationId,
            cancellationToken: cancellationToken);

        if (configuration is null)
            throw new BusinessException(OrganizationMailConfigurationsMessages.ConfigurationNotFound);

        return configuration;
    }
}
