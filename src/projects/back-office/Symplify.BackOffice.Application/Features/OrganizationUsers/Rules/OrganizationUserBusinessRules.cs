using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.OrganizationUsers.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;
namespace Symplify.BackOffice.Application.Features.OrganizationUsers.Rules;
public class OrganizationUserBusinessRules : BaseBusinessRules
{
    public Task OrganizationUserShouldExistWhenSelected(OrganizationUser? entity)
    {
        if (entity is null) throw new BusinessException(OrganizationUsersMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
