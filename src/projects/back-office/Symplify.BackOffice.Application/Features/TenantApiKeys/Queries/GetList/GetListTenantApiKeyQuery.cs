using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.TenantApiKeys.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Tenant;
namespace Symplify.BackOffice.Application.Features.TenantApiKeys.Queries.GetList;
public class GetListTenantApiKeyQuery : IRequest<GetListResponse<GetListTenantApiKeyListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { TenantApiKeysOperationClaims.Admin, TenantApiKeysOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListTenantApiKeys({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetTenantApiKeys";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListTenantApiKeyQueryHandler : IRequestHandler<GetListTenantApiKeyQuery, GetListResponse<GetListTenantApiKeyListItemDto>>
    {
        private readonly ITenantApiKeyRepository _repository; private readonly IMapper _mapper;
        public GetListTenantApiKeyQueryHandler(ITenantApiKeyRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListTenantApiKeyListItemDto>> Handle(GetListTenantApiKeyQuery request, CancellationToken cancellationToken)
        {
            IPaginate<TenantApiKey> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListTenantApiKeyListItemDto>>(entities);
        }
    }
}
