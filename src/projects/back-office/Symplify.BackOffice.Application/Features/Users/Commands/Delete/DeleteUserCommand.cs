using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Users.Constants;
using Symplify.BackOffice.Application.Services.UserAdministration;

namespace Symplify.BackOffice.Application.Features.Users.Commands.Delete;

public sealed class DeleteUserCommand : IRequest, ISecuredRequest
{
    public Guid UserId { get; set; }

    public string[] Roles => new[]
    {
        "SuperAdmin",
        "OrganizationAdmin",
        UsersOperationClaims.Admin,
        UsersOperationClaims.Delete
    };

    public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
    {
        private readonly IUserAdministrationService _service;

        public DeleteUserCommandHandler(IUserAdministrationService service)
        {
            _service = service;
        }

        public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            await _service.SoftDeleteAsync(request.UserId, cancellationToken);
        }
    }
}
