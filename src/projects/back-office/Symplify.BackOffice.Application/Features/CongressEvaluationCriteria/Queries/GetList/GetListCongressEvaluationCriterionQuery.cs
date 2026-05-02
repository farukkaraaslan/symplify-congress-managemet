using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressEvaluationCriteria.Queries.GetList;
public class GetListCongressEvaluationCriterionQuery : IRequest<GetListResponse<GetListCongressEvaluationCriterionListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { CongressEvaluationCriteriaOperationClaims.Admin, CongressEvaluationCriteriaOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListCongressEvaluationCriteria({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetCongressEvaluationCriteria";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListCongressEvaluationCriterionQueryHandler : IRequestHandler<GetListCongressEvaluationCriterionQuery, GetListResponse<GetListCongressEvaluationCriterionListItemDto>>
    {
        private readonly ICongressEvaluationCriterionRepository _repository; private readonly IMapper _mapper;
        public GetListCongressEvaluationCriterionQueryHandler(ICongressEvaluationCriterionRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListCongressEvaluationCriterionListItemDto>> Handle(GetListCongressEvaluationCriterionQuery request, CancellationToken cancellationToken)
        {
            IPaginate<CongressEvaluationCriterion> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListCongressEvaluationCriterionListItemDto>>(entities);
        }
    }
}
