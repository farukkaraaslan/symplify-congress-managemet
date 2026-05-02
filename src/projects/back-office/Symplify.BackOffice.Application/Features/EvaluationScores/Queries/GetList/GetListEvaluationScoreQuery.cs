using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.EvaluationScores.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.EvaluationScores.Queries.GetList;
public class GetListEvaluationScoreQuery : IRequest<GetListResponse<GetListEvaluationScoreListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { EvaluationScoresOperationClaims.Admin, EvaluationScoresOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListEvaluationScores({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetEvaluationScores";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListEvaluationScoreQueryHandler : IRequestHandler<GetListEvaluationScoreQuery, GetListResponse<GetListEvaluationScoreListItemDto>>
    {
        private readonly IEvaluationScoreRepository _repository; private readonly IMapper _mapper;
        public GetListEvaluationScoreQueryHandler(IEvaluationScoreRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListEvaluationScoreListItemDto>> Handle(GetListEvaluationScoreQuery request, CancellationToken cancellationToken)
        {
            IPaginate<EvaluationScore> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListEvaluationScoreListItemDto>>(entities);
        }
    }
}
