using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Roles.Constants;
using Symplify.BackOffice.Application.Features.Roles.Dtos;
using Symplify.BackOffice.Application.Services.RoleAdministration;

namespace Symplify.BackOffice.Application.Features.Roles.Queries.GetById;

public sealed class GetByIdRoleQuery : IRequest<RoleDetailDto>, ISecuredRequest
{
    public Guid Id { get; set; }

    public string[] Roles => new[]
    {
        "SuperAdmin",
        RolesOperationClaims.Admin,
        RolesOperationClaims.Read
    };

    public sealed class Handler : IRequestHandler<GetByIdRoleQuery, RoleDetailDto>
    {
        private readonly IRoleAdministrationService _service;

        public Handler(IRoleAdministrationService service)
        {
            _service = service;
        }

        public Task<RoleDetailDto> Handle(GetByIdRoleQuery request, CancellationToken cancellationToken)
        {
            return _service.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}
