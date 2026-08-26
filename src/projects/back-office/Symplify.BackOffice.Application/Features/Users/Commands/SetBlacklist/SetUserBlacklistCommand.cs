using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Users.Constants;
using Symplify.BackOffice.Application.Services.UserAdministration;

namespace Symplify.BackOffice.Application.Features.Users.Commands.SetBlacklist;

public sealed class SetUserBlacklistCommand : IRequest, ISecuredRequest
{
    public Guid UserId { get; set; }
    public bool IsBlacklisted { get; set; }

    public string[] Roles => new[]
    {
        "SuperAdmin",
        "OrganizationAdmin",
        UsersOperationClaims.Admin,
        UsersOperationClaims.Blacklist
    };

    public sealed class SetUserBlacklistCommandHandler : IRequestHandler<SetUserBlacklistCommand>
    {
        private readonly IUserAdministrationService _service;

        public SetUserBlacklistCommandHandler(IUserAdministrationService service)
        {
            _service = service;
        }

        public async Task Handle(SetUserBlacklistCommand request, CancellationToken cancellationToken)
        {
            await _service.SetBlacklistAsync(request.UserId, request.IsBlacklisted, cancellationToken);
        }
    }
}
