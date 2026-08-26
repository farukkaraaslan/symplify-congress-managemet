using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Roles.Constants;
using Symplify.BackOffice.Application.Services.RoleAdministration;

namespace Symplify.BackOffice.Application.Features.Roles.Commands.UpdateClaims;

public sealed class UpdateRoleClaimsCommand : IRequest, ISecuredRequest
{
    public Guid RoleId { get; set; }
    public List<string> ClaimNames { get; set; } = new();

    public string[] Roles => new[]
    {
        "SuperAdmin",
        RolesOperationClaims.Admin,
        RolesOperationClaims.ManageClaims
    };

    public sealed class Handler : IRequestHandler<UpdateRoleClaimsCommand>
    {
        private readonly IRoleAdministrationService _service;

        public Handler(IRoleAdministrationService service)
        {
            _service = service;
        }

        public async Task Handle(UpdateRoleClaimsCommand request, CancellationToken cancellationToken)
        {
            await _service.UpdateClaimsAsync(request.RoleId, request.ClaimNames, cancellationToken);
        }
    }
}
