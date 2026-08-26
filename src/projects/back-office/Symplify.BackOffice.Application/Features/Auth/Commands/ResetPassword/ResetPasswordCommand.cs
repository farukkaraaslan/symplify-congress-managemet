using MediatR;
using Symplify.BackOffice.Application.Services.Authentication;

namespace Symplify.BackOffice.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommand : IRequest<ResetPasswordResponse>
{
    public string Email { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string ConfirmPassword { get; set; } = string.Empty;

    public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
    {
        private readonly IBackOfficeIdentityService _identityService;

        public ResetPasswordCommandHandler(IBackOfficeIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<ResetPasswordResponse> Handle(
            ResetPasswordCommand request,
            CancellationToken cancellationToken)
        {
            await _identityService.ResetPasswordAsync(
                request.Email,
                request.Token,
                request.Password,
                cancellationToken);

            return new ResetPasswordResponse
            {
                Email = request.Email
            };
        }
    }
}
