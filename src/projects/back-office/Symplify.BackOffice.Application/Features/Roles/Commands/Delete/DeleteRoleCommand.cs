using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Roles.Constants;
using Symplify.BackOffice.Application.Services.RoleAdministration;

namespace Symplify.BackOffice.Application.Features.Roles.Commands.Delete;

public sealed class DeleteRoleCommand : IRequest, ISecuredRequest
{
    public Guid RoleId { get; set; }

    public string[] Roles => new[]
    {
        "SuperAdmin",
        RolesOperationClaims.Admin,
        RolesOperationClaims.Delete
    };

    public sealed class Handler : IRequestHandler<DeleteRoleCommand>
    {
        private readonly IRoleAdministrationService _service;

        public Handler(IRoleAdministrationService service)
        {
            _service = service;
        }

        public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(request.RoleId, cancellationToken);
        }
    }
}
