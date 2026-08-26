using Core.Application.Pipelines.Authorization;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Symplify.BackOffice.Application.Features.Roles.Constants;
using Symplify.BackOffice.Application.Features.Roles.Dtos;
using Symplify.BackOffice.Application.Services.RoleAdministration;

namespace Symplify.BackOffice.Application.Features.Roles.Queries.GetList;

public sealed class GetListRoleQuery : IRequest<GetListResponse<RoleListItemDto>>, ISecuredRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string? SearchText { get; set; }

    public string[] Roles => new[]
    {
        "SuperAdmin",
        RolesOperationClaims.Admin,
        RolesOperationClaims.Read
    };

    public sealed class Handler : IRequestHandler<GetListRoleQuery, GetListResponse<RoleListItemDto>>
    {
        private readonly IRoleAdministrationService _service;

        public Handler(IRoleAdministrationService service)
        {
            _service = service;
        }

        public Task<GetListResponse<RoleListItemDto>> Handle(GetListRoleQuery request, CancellationToken cancellationToken)
        {
            return _service.GetListAsync(
                request.PageRequest.Page,
                request.PageRequest.PageSize,
                request.SearchText,
                cancellationToken);
        }
    }
}
