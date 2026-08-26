using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.SubmissionEvaluations.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.SubmissionEvaluations.Rules;
public class SubmissionEvaluationBusinessRules : BaseBusinessRules
{
    public Task SubmissionEvaluationShouldExistWhenSelected(SubmissionEvaluation? entity)
    {
        if (entity is null) throw new BusinessException(SubmissionEvaluationsMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
