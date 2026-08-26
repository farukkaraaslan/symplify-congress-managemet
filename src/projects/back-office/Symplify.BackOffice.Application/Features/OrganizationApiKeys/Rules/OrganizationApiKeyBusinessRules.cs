using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.OrganizationApiKeys.Rules;

public class OrganizationApiKeyBusinessRules : BaseBusinessRules
{
    private static readonly string[] ValidEnvironments = { "Production", "Sandbox", "Development" };
    private static readonly string[] ValidKeyTypes = { "SecretKey", "PublicKey", "IntegrationKey" };

    private readonly IOrganizationApiKeyRepository _organizationApiKeyRepository;
    private readonly IOrganizationRepository _organizationRepository;

    public OrganizationApiKeyBusinessRules(
        IOrganizationApiKeyRepository organizationApiKeyRepository,
        IOrganizationRepository organizationRepository)
    {
        _organizationApiKeyRepository = organizationApiKeyRepository;
        _organizationRepository = organizationRepository;
    }

    public Task OrganizationApiKeyIdShouldBeValid(Guid id)
    {
        if (id == Guid.Empty)
            throw new BusinessException(OrganizationApiKeysMessages.InvalidRequest);

        return Task.CompletedTask;
    }

    public Task OrganizationIdShouldBeValid(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
            throw new BusinessException(OrganizationApiKeysMessages.OrganizationNotFound);

        return Task.CompletedTask;
    }

    public Task OrganizationApiKeyShouldExistWhenSelected(OrganizationApiKey? entity)
    {
        if (entity is null)
            throw new BusinessException(OrganizationApiKeysMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public Task OrganizationShouldExistWhenSelected(Organization? entity)
    {
        if (entity is null)
            throw new BusinessException(OrganizationApiKeysMessages.OrganizationNotFound);

        return Task.CompletedTask;
    }

    public async Task<Organization> GetExistingOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await OrganizationIdShouldBeValid(organizationId);

        Organization? organization = await _organizationRepository.GetAsync(
            predicate: entity => entity.Id == organizationId,
            cancellationToken: cancellationToken);

        await OrganizationShouldExistWhenSelected(organization);

        return organization!;
    }

    public Task OrganizationShouldBeActiveWhenCreatingApiKey(Organization organization)
    {
        if (!organization.IsActive)
            throw new BusinessException(OrganizationApiKeysMessages.OrganizationPassive);

        return Task.CompletedTask;
    }

    public Task OrganizationShouldBeActiveWhenActivatingApiKey(Organization organization, bool requestedIsActive)
    {
        if (requestedIsActive && !organization.IsActive)
            throw new BusinessException(OrganizationApiKeysMessages.OrganizationPassive);

        return Task.CompletedTask;
    }

    public async Task OrganizationApiKeyNameShouldBeUniqueWhenCreating(
        Guid organizationId,
        string name,
        CancellationToken cancellationToken)
    {
        string normalizedName = NormalizeRequired(name);

        OrganizationApiKey? existing = await _organizationApiKeyRepository.GetAsync(
            predicate: entity => entity.OrganizationId == organizationId && entity.Name.ToLower() == normalizedName.ToLower(),
            cancellationToken: cancellationToken);

        if (existing is not null)
            throw new BusinessException(OrganizationApiKeysMessages.NameAlreadyExists);
    }

    public async Task OrganizationApiKeyNameShouldBeUniqueWhenUpdating(
        Guid id,
        Guid organizationId,
        string name,
        CancellationToken cancellationToken)
    {
        string normalizedName = NormalizeRequired(name);

        OrganizationApiKey? existing = await _organizationApiKeyRepository.GetAsync(
            predicate: entity => entity.Id != id && entity.OrganizationId == organizationId && entity.Name.ToLower() == normalizedName.ToLower(),
            cancellationToken: cancellationToken);

        if (existing is not null)
            throw new BusinessException(OrganizationApiKeysMessages.NameAlreadyExists);
    }

    public Task OrganizationShouldNotChangeWhenUpdating(Guid currentOrganizationId, Guid requestedOrganizationId)
    {
        if (requestedOrganizationId != Guid.Empty && requestedOrganizationId != currentOrganizationId)
            throw new BusinessException(OrganizationApiKeysMessages.OrganizationCannotBeChanged);

        return Task.CompletedTask;
    }

    public Task RevokedApiKeyShouldNotBeUpdated(OrganizationApiKey entity)
    {
        if (entity.RevokedAt is not null)
            throw new BusinessException(OrganizationApiKeysMessages.RevokedApiKeyCannotBeUpdated);

        return Task.CompletedTask;
    }

    public Task EnvironmentShouldBeValid(string? environment)
    {
        string value = NormalizeRequired(environment, "Production");

        if (!ValidEnvironments.Contains(value, StringComparer.OrdinalIgnoreCase))
            throw new BusinessException(OrganizationApiKeysMessages.InvalidEnvironment);

        return Task.CompletedTask;
    }

    public Task KeyTypeShouldBeValid(string? keyType)
    {
        string value = NormalizeRequired(keyType, "SecretKey");

        if (!ValidKeyTypes.Contains(value, StringComparer.OrdinalIgnoreCase))
            throw new BusinessException(OrganizationApiKeysMessages.InvalidKeyType);

        return Task.CompletedTask;
    }

    public Task ScopesShouldBeValid(IEnumerable<string>? scopes)
    {
        string[] invalidScopes = (scopes ?? Array.Empty<string>())
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .Where(scope => !OrganizationApiKeyScopes.All.Contains(scope, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (invalidScopes.Length > 0)
            throw new BusinessException(OrganizationApiKeysMessages.InvalidScope);

        return Task.CompletedTask;
    }

    public Task ExpiresAtShouldBeFutureWhenSelected(DateTime? expiresAt)
    {
        if (expiresAt.HasValue && ToUtc(expiresAt.Value) <= DateTime.UtcNow)
            throw new BusinessException(OrganizationApiKeysMessages.ExpiresAtMustBeFuture);

        return Task.CompletedTask;
    }

    private static string NormalizeRequired(string? value, string fallback = "")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
        };
    }
}
