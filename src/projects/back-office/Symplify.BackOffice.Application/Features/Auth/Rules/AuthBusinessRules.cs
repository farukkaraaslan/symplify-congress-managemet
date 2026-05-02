using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.Auth.Constants;
using Symplify.BackOffice.Application.Services.Authentication;

namespace Symplify.BackOffice.Application.Features.Auth.Rules;

public sealed class AuthBusinessRules : BaseBusinessRules
{
    public Task UserShouldExistWhenLogin(AuthenticatedUserDto? user)
    {
        if (user is null)
            throw new BusinessException(AuthMessages.UserNotFoundOrPasswordInvalid);

        return Task.CompletedTask;
    }
}
