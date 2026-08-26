using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.Reviewers.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Features.Reviewers.Rules;

public class ReviewerBusinessRules : BaseBusinessRules
{
    private readonly IReviewerRepository _reviewerRepository;

    public ReviewerBusinessRules(IReviewerRepository reviewerRepository)
    {
        _reviewerRepository = reviewerRepository;
    }

    public Task ReviewerShouldExistWhenSelected(Reviewer? entity)
    {
        if (entity is null) throw new BusinessException(ReviewersMessages.EntityNotFound);
        return Task.CompletedTask;
    }

    public async Task ReviewerShouldNotExistForUser(Guid userId)
    {
        Reviewer? existingReviewer = await _reviewerRepository.GetAsync(
            predicate: reviewer => reviewer.UserId == userId && reviewer.DeletedDate == null);

        if (existingReviewer is not null)
            throw new BusinessException(ReviewersMessages.UserAlreadyReviewer);
    }
}
