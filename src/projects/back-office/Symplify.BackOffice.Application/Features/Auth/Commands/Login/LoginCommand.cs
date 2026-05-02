using MediatR;
using Symplify.BackOffice.Application.Services.Authentication;

namespace Symplify.BackOffice.Application.Features.Auth.Commands.Login;

public sealed class LoginCommand : IRequest<LoggedInResponse>
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoggedInResponse>
    {
        private readonly IBackOfficeIdentityService _identityService;

        public LoginCommandHandler(IBackOfficeIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<LoggedInResponse> Handle(
            LoginCommand request,
            CancellationToken cancellationToken)
        {
            AuthenticatedUserDto user = await _identityService.AuthenticateAsync(
                request.Email,
                request.Password,
                cancellationToken);

            return new LoggedInResponse
            {
                User = user
            };
        }
    }
}