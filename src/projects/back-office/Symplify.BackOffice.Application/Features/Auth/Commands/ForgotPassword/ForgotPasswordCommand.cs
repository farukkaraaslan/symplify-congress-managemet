using MediatR;
using Symplify.BackOffice.Application.Services.Authentication;

namespace Symplify.BackOffice.Application.Features.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommand : IRequest<ForgotPasswordResponse>
{
    public string Email { get; set; } = string.Empty;

    public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
    {
        private readonly IBackOfficeIdentityService _identityService;

        public ForgotPasswordCommandHandler(IBackOfficeIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<ForgotPasswordResponse> Handle(
            ForgotPasswordCommand request,
            CancellationToken cancellationToken)
        {
            PasswordResetTokenDto resetToken = await _identityService.GeneratePasswordResetTokenAsync(
                request.Email,
                cancellationToken);

            return new ForgotPasswordResponse
            {
                UserId = resetToken.UserId,
                TokenGenerated = resetToken.TokenGenerated,
                Email = resetToken.Email,
                Token = resetToken.Token,
                DisplayName = resetToken.DisplayName
            };
        }
    }
}
