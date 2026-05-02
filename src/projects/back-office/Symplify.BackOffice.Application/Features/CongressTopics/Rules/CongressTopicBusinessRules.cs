using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.CongressTopics.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressTopics.Rules;
public class CongressTopicBusinessRules : BaseBusinessRules
{
    public Task CongressTopicShouldExistWhenSelected(CongressTopic? entity)
    {
        if (entity is null) throw new BusinessException(CongressTopicsMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
