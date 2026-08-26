using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Roles.Constants;
using Symplify.BackOffice.Application.Features.Roles.Dtos;
using Symplify.BackOffice.Application.Services.RoleAdministration;

namespace Symplify.BackOffice.Application.Features.Roles.Commands.Create;

public sealed class CreateRoleCommand : IRequest<RoleDetailDto>, ISecuredRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> ClaimNames { get; set; } = new();

    public string[] Roles => new[]
    {
        "SuperAdmin",
        RolesOperationClaims.Admin,
        RolesOperationClaims.Add
    };

    public sealed class Handler : IRequestHandler<CreateRoleCommand, RoleDetailDto>
    {
        private readonly IRoleAdministrationService _service;

        public Handler(IRoleAdministrationService service)
        {
            _service = service;
        }

        public Task<RoleDetailDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            return _service.CreateAsync(new CreateRoleRequestDto
            {
                Name = request.Name,
                Description = request.Description,
                ClaimNames = request.ClaimNames
            }, cancellationToken);
        }
    }
}
