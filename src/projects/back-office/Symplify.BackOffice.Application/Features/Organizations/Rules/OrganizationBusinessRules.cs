using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.Organizations.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.Organizations.Rules;

public class OrganizationBusinessRules : BaseBusinessRules
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ICongressRepository _congressRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;

    public OrganizationBusinessRules(
        IOrganizationRepository organizationRepository,
        ICongressRepository congressRepository,
        IOrganizationUserRepository organizationUserRepository)
    {
        _organizationRepository = organizationRepository;
        _congressRepository = congressRepository;
        _organizationUserRepository = organizationUserRepository;
    }

    public Task OrganizationIdShouldBeValid(Guid id)
    {
        if (id == Guid.Empty)
            throw new BusinessException(OrganizationsMessages.InvalidOrganizationId);

        return Task.CompletedTask;
    }

    public Task OrganizationShouldExistWhenSelected(Organization? entity)
    {
        if (entity is null)
            throw new BusinessException(OrganizationsMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public Task OrganizationCodeShouldBeUniqueWhenCreating(string code)
    {
        string normalizedCode = NormalizeCode(code);

        bool exists = _organizationRepository
            .Query()
            .Any(organization => organization.Code == normalizedCode);

        if (exists)
            throw new BusinessException(OrganizationsMessages.CodeAlreadyExists);

        return Task.CompletedTask;
    }

    public Task OrganizationCodeShouldBeUniqueWhenUpdating(Guid organizationId, string code)
    {
        string normalizedCode = NormalizeCode(code);

        bool exists = _organizationRepository
            .Query()
            .Any(organization => organization.Id != organizationId && organization.Code == normalizedCode);

        if (exists)
            throw new BusinessException(OrganizationsMessages.CodeAlreadyExists);

        return Task.CompletedTask;
    }

    public Task OrganizationSlugShouldBeUniqueWhenCreating(string slug)
    {
        string normalizedSlug = NormalizeCode(slug);

        bool exists = _organizationRepository
            .Query()
            .Any(organization => organization.Slug == normalizedSlug);

        if (exists)
            throw new BusinessException(OrganizationsMessages.SlugAlreadyExists);

        return Task.CompletedTask;
    }

    public Task OrganizationSlugShouldBeUniqueWhenUpdating(Guid organizationId, string slug)
    {
        string normalizedSlug = NormalizeCode(slug);

        bool exists = _organizationRepository
            .Query()
            .Any(organization => organization.Id != organizationId && organization.Slug == normalizedSlug);

        if (exists)
            throw new BusinessException(OrganizationsMessages.SlugAlreadyExists);

        return Task.CompletedTask;
    }

    public Task OrganizationShouldNotHaveRelatedCongressesWhenDeleting(Guid organizationId)
    {
        bool hasCongress = _congressRepository
            .Query()
            .Any(congress => congress.OrganizationId == organizationId);

        if (hasCongress)
            throw new BusinessException(OrganizationsMessages.OrganizationHasCongressesCannotBeDeleted);

        return Task.CompletedTask;
    }

    public Task OrganizationShouldNotHaveRelatedUsersWhenDeleting(Guid organizationId)
    {
        bool hasUser = _organizationUserRepository
            .Query()
            .Any(user => user.OrganizationId == organizationId);

        if (hasUser)
            throw new BusinessException(OrganizationsMessages.OrganizationHasUsersCannotBeDeleted);

        return Task.CompletedTask;
    }

    private static string NormalizeCode(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}
