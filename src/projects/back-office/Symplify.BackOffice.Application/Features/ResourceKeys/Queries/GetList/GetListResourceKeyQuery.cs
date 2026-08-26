using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.ResourceKeys.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.ResourceKeys.Queries.GetList;
public class GetListResourceKeyQuery : IRequest<GetListResponse<GetListResourceKeyListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { ResourceKeysOperationClaims.Admin, ResourceKeysOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListResourceKeys({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetResourceKeys";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListResourceKeyQueryHandler : IRequestHandler<GetListResourceKeyQuery, GetListResponse<GetListResourceKeyListItemDto>>
    {
        private readonly IResourceKeyRepository _repository; private readonly IMapper _mapper;
        public GetListResourceKeyQueryHandler(IResourceKeyRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListResourceKeyListItemDto>> Handle(GetListResourceKeyQuery request, CancellationToken cancellationToken)
        {
            IPaginate<ResourceKey> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListResourceKeyListItemDto>>(entities);
        }
    }
}
