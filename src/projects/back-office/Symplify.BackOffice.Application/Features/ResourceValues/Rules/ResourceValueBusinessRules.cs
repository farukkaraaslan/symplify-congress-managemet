using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.ResourceValues.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.ResourceValues.Rules;
public class ResourceValueBusinessRules : BaseBusinessRules
{
    public Task ResourceValueShouldExistWhenSelected(ResourceValue? entity)
    {
        if (entity is null) throw new BusinessException(ResourceValuesMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
