using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.TenantUsers.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.TenantUsers.Queries.GetList;
public class GetListTenantUserQuery : IRequest<GetListResponse<GetListTenantUserListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { TenantUsersOperationClaims.Admin, TenantUsersOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListTenantUsers({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetTenantUsers";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListTenantUserQueryHandler : IRequestHandler<GetListTenantUserQuery, GetListResponse<GetListTenantUserListItemDto>>
    {
        private readonly ITenantUserRepository _repository; private readonly IMapper _mapper;
        public GetListTenantUserQueryHandler(ITenantUserRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListTenantUserListItemDto>> Handle(GetListTenantUserQuery request, CancellationToken cancellationToken)
        {
            IPaginate<TenantUser> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListTenantUserListItemDto>>(entities);
        }
    }
}
