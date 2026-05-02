using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Submissions.Rules;
public class SubmissionBusinessRules : BaseBusinessRules
{
    public Task SubmissionShouldExistWhenSelected(Submission? entity)
    {
        if (entity is null) throw new BusinessException(SubmissionsMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
