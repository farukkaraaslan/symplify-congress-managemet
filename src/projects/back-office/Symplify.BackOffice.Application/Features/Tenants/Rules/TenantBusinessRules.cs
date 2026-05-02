using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.Tenants.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.Tenants.Rules;
public class TenantBusinessRules : BaseBusinessRules
{
    public Task TenantShouldExistWhenSelected(Tenant? entity)
    {
        if (entity is null) throw new BusinessException(TenantsMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
