using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.SubmissionHistories.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.SubmissionHistories.Rules;
public class SubmissionHistoryBusinessRules : BaseBusinessRules
{
    public Task SubmissionHistoryShouldExistWhenSelected(SubmissionHistory? entity)
    {
        if (entity is null) throw new BusinessException(SubmissionHistoriesMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
