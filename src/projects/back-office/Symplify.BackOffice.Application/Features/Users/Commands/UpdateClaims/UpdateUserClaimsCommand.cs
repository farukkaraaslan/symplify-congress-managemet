using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Users.Constants;
using Symplify.BackOffice.Application.Services.UserAdministration;

namespace Symplify.BackOffice.Application.Features.Users.Commands.UpdateClaims;

public sealed class UpdateUserClaimsCommand : IRequest, ISecuredRequest
{
    public Guid UserId { get; set; }
    public List<string> ClaimNames { get; set; } = new();

    public string[] Roles => new[]
    {
        "SuperAdmin",
        UsersOperationClaims.Admin,
        UsersOperationClaims.ManageClaims
    };

    public sealed class UpdateUserClaimsCommandHandler : IRequestHandler<UpdateUserClaimsCommand>
    {
        private readonly IUserAdministrationService _service;

        public UpdateUserClaimsCommandHandler(IUserAdministrationService service)
        {
            _service = service;
        }

        public async Task Handle(UpdateUserClaimsCommand request, CancellationToken cancellationToken)
        {
            await _service.UpdateClaimsAsync(request.UserId, request.ClaimNames, cancellationToken);
        }
    }
}
