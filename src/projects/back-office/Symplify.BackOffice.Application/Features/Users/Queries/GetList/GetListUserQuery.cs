using Core.Application.Pipelines.Authorization;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Symplify.BackOffice.Application.Features.Users.Constants;
using Symplify.BackOffice.Application.Features.Users.Dtos;
using Symplify.BackOffice.Application.Services.UserAdministration;

namespace Symplify.BackOffice.Application.Features.Users.Queries.GetList;

public sealed class GetListUserQuery : IRequest<GetListResponse<UserListItemDto>>, ISecuredRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string? SearchText { get; set; }
    public bool? IsBlacklisted { get; set; }
    public Guid? OrganizationId { get; set; }
    public bool? EmailConfirmed { get; set; }
    public Guid? CountryId { get; set; }
    public Guid? StateId { get; set; }
    public Guid? CongressId { get; set; }
    public string? RoleName { get; set; }
    public string? AccountStatus { get; set; }
    public string? Culture { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }

    public string[] Roles => new[]
    {
        "SuperAdmin",
        "OrganizationAdmin",
        UsersOperationClaims.Admin,
        UsersOperationClaims.Read
    };

    public sealed class GetListUserQueryHandler : IRequestHandler<GetListUserQuery, GetListResponse<UserListItemDto>>
    {
        private readonly IUserAdministrationService _service;

        public GetListUserQueryHandler(IUserAdministrationService service)
        {
            _service = service;
        }

        public Task<GetListResponse<UserListItemDto>> Handle(GetListUserQuery request, CancellationToken cancellationToken)
        {
            return _service.GetListAsync(
                request.PageRequest.Page,
                request.PageRequest.PageSize,
                request.SearchText,
                request.IsBlacklisted,
                request.OrganizationId,
                request.EmailConfirmed,
                request.CountryId,
                request.StateId,
                request.CongressId,
                request.RoleName,
                request.AccountStatus,
                request.Culture,
                request.SortColumn,
                request.SortDirection,
                cancellationToken);
        }
    }
}
