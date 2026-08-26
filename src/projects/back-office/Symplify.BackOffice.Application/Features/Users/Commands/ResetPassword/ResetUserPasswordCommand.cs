using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Users.Constants;
using Symplify.BackOffice.Application.Features.Users.Dtos;
using Symplify.BackOffice.Application.Services.UserAdministration;

namespace Symplify.BackOffice.Application.Features.Users.Commands.ResetPassword;

public sealed class ResetUserPasswordCommand : IRequest<ResetUserPasswordDto>, ISecuredRequest
{
    public Guid UserId { get; set; }

    public string[] Roles => new[]
    {
        "SuperAdmin",
        "OrganizationAdmin",
        UsersOperationClaims.Admin,
        UsersOperationClaims.ResetPassword
    };

    public sealed class ResetUserPasswordCommandHandler : IRequestHandler<ResetUserPasswordCommand, ResetUserPasswordDto>
    {
        private readonly IUserAdministrationService _service;

        public ResetUserPasswordCommandHandler(IUserAdministrationService service)
        {
            _service = service;
        }

        public Task<ResetUserPasswordDto> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
        {
            return _service.ResetPasswordAsync(request.UserId, cancellationToken);
        }
    }
}
