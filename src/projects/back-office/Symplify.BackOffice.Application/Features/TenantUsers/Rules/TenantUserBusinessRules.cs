using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.TenantUsers.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.TenantUsers.Rules;
public class TenantUserBusinessRules : BaseBusinessRules
{
    public Task TenantUserShouldExistWhenSelected(TenantUser? entity)
    {
        if (entity is null) throw new BusinessException(TenantUsersMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
