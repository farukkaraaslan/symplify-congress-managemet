using MediatR;
using Symplify.BackOffice.Application.Services.Authentication;

namespace Symplify.BackOffice.Application.Features.Auth.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommand : IRequest<ConfirmEmailResponse>
{
    public string Email { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, ConfirmEmailResponse>
    {
        private readonly IBackOfficeIdentityService _identityService;

        public ConfirmEmailCommandHandler(IBackOfficeIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<ConfirmEmailResponse> Handle(
            ConfirmEmailCommand request,
            CancellationToken cancellationToken)
        {
            await _identityService.ConfirmEmailAsync(
                request.Email,
                request.Token,
                cancellationToken);

            return new ConfirmEmailResponse
            {
                Email = request.Email,
                Success = true
            };
        }
    }
}
