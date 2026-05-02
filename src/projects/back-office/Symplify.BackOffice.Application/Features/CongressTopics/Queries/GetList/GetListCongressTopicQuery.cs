using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressTopics.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressTopics.Queries.GetList;
public class GetListCongressTopicQuery : IRequest<GetListResponse<GetListCongressTopicListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { CongressTopicsOperationClaims.Admin, CongressTopicsOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListCongressTopics({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetCongressTopics";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListCongressTopicQueryHandler : IRequestHandler<GetListCongressTopicQuery, GetListResponse<GetListCongressTopicListItemDto>>
    {
        private readonly ICongressTopicRepository _repository; private readonly IMapper _mapper;
        public GetListCongressTopicQueryHandler(ICongressTopicRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListCongressTopicListItemDto>> Handle(GetListCongressTopicQuery request, CancellationToken cancellationToken)
        {
            IPaginate<CongressTopic> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListCongressTopicListItemDto>>(entities);
        }
    }
}
