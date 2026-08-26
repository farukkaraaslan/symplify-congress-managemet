using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.Authors.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Authors.Rules;
public class AuthorBusinessRules : BaseBusinessRules
{
    public Task AuthorShouldExistWhenSelected(Author? entity)
    {
        if (entity is null) throw new BusinessException(AuthorsMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
