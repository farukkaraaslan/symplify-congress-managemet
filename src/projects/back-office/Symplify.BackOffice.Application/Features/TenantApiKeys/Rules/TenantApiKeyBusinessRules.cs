using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.TenantApiKeys.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.TenantApiKeys.Rules;
public class TenantApiKeyBusinessRules : BaseBusinessRules
{
    public Task TenantApiKeyShouldExistWhenSelected(TenantApiKey? entity)
    {
        if (entity is null) throw new BusinessException(TenantApiKeysMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
