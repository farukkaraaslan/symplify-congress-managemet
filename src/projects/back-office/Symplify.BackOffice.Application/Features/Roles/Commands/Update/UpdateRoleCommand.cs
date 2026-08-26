using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.Roles.Constants;
using Symplify.BackOffice.Application.Features.Roles.Dtos;
using Symplify.BackOffice.Application.Services.RoleAdministration;

namespace Symplify.BackOffice.Application.Features.Roles.Commands.Update;

public sealed class UpdateRoleCommand : IRequest<RoleDetailDto>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string[] Roles => new[]
    {
        "SuperAdmin",
        RolesOperationClaims.Admin,
        RolesOperationClaims.Update
    };

    public sealed class Handler : IRequestHandler<UpdateRoleCommand, RoleDetailDto>
    {
        private readonly IRoleAdministrationService _service;

        public Handler(IRoleAdministrationService service)
        {
            _service = service;
        }

        public Task<RoleDetailDto> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            return _service.UpdateAsync(new UpdateRoleRequestDto
            {
                Id = request.Id,
                Name = request.Name,
                Description = request.Description
            }, cancellationToken);
        }
    }
}
