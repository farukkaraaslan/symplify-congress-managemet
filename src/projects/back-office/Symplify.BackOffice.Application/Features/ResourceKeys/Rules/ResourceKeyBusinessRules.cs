using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.ResourceKeys.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.ResourceKeys.Rules;
public class ResourceKeyBusinessRules : BaseBusinessRules
{
    public Task ResourceKeyShouldExistWhenSelected(ResourceKey? entity)
    {
        if (entity is null) throw new BusinessException(ResourceKeysMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
