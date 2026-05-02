using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Rules;
public class CongressSubmissionTypeBusinessRules : BaseBusinessRules
{
    public Task CongressSubmissionTypeShouldExistWhenSelected(CongressSubmissionType? entity)
    {
        if (entity is null) throw new BusinessException(CongressSubmissionTypesMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
