using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.Reviewers.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Reviewers.Rules;
public class ReviewerBusinessRules : BaseBusinessRules
{
    public Task ReviewerShouldExistWhenSelected(Reviewer? entity)
    {
        if (entity is null) throw new BusinessException(ReviewersMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
