using Symplify.BackOffice.Application.Services.Authentication;

namespace Symplify.BackOffice.Application.Features.Auth.Commands.Login;

public sealed class LoggedInResponse
{
    public AuthenticatedUserDto User { get; set; } = default!;
}
