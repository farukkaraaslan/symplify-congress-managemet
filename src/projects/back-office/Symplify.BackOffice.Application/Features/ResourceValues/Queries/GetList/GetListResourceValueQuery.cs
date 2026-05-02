using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.ResourceValues.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.ResourceValues.Queries.GetList;
public class GetListResourceValueQuery : IRequest<GetListResponse<GetListResourceValueListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { ResourceValuesOperationClaims.Admin, ResourceValuesOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListResourceValues({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetResourceValues";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListResourceValueQueryHandler : IRequestHandler<GetListResourceValueQuery, GetListResponse<GetListResourceValueListItemDto>>
    {
        private readonly IResourceValueRepository _repository; private readonly IMapper _mapper;
        public GetListResourceValueQueryHandler(IResourceValueRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListResourceValueListItemDto>> Handle(GetListResourceValueQuery request, CancellationToken cancellationToken)
        {
            IPaginate<ResourceValue> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListResourceValueListItemDto>>(entities);
        }
    }
}
