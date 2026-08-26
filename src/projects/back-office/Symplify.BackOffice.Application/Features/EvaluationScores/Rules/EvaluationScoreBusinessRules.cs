using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.EvaluationScores.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.EvaluationScores.Rules;
public class EvaluationScoreBusinessRules : BaseBusinessRules
{
    public Task EvaluationScoreShouldExistWhenSelected(EvaluationScore? entity)
    {
        if (entity is null) throw new BusinessException(EvaluationScoresMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
