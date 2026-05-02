using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.Tenants.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.Tenants.Queries.GetList;
public class GetListTenantQuery : IRequest<GetListResponse<GetListTenantListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { TenantsOperationClaims.Admin, TenantsOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListTenants({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetTenants";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListTenantQueryHandler : IRequestHandler<GetListTenantQuery, GetListResponse<GetListTenantListItemDto>>
    {
        private readonly ITenantRepository _repository; private readonly IMapper _mapper;
        public GetListTenantQueryHandler(ITenantRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListTenantListItemDto>> Handle(GetListTenantQuery request, CancellationToken cancellationToken)
        {
            IPaginate<Tenant> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListTenantListItemDto>>(entities);
        }
    }
}
