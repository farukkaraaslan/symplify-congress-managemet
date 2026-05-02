using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.Languages.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.Languages.Rules;
public class LanguageBusinessRules : BaseBusinessRules
{
    public Task LanguageShouldExistWhenSelected(Language? entity)
    {
        if (entity is null) throw new BusinessException(LanguagesMessages.EntityNotFound);
        return Task.CompletedTask;
    }
}
