using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Rules;
public class CongressEvaluationCriterionBusinessRules : BaseBusinessRules
{
    public Task CongressEvaluationCriterionShouldExistWhenSelected(CongressEvaluationCriterion? entity)
    {
        if (entity is null) throw new BusinessException(CongressEvaluationCriteriaMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
