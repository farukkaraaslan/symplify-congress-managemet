using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Users.Constants;
using Symplify.BackOffice.Application.Services.UserAdministration;

namespace Symplify.BackOffice.Application.Features.Users.Commands.UpdateRoles;

public sealed class UpdateUserRolesCommand : IRequest, ISecuredRequest
{
    public Guid UserId { get; set; }
    public List<string> RoleNames { get; set; } = new();

    public string[] Roles => new[]
    {
        "SuperAdmin",
        "OrganizationAdmin",
        UsersOperationClaims.Admin,
        UsersOperationClaims.ManageRoles
    };

    public sealed class UpdateUserRolesCommandHandler : IRequestHandler<UpdateUserRolesCommand>
    {
        private readonly IUserAdministrationService _service;

        public UpdateUserRolesCommandHandler(IUserAdministrationService service)
        {
            _service = service;
        }

        public async Task Handle(UpdateUserRolesCommand request, CancellationToken cancellationToken)
        {
            await _service.UpdateRolesAsync(request.UserId, request.RoleNames, cancellationToken);
        }
    }
}
