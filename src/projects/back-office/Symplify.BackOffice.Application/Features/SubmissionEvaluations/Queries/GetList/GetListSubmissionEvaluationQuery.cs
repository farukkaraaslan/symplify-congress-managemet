using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.SubmissionEvaluations.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.SubmissionEvaluations.Queries.GetList;
public class GetListSubmissionEvaluationQuery : IRequest<GetListResponse<GetListSubmissionEvaluationListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { SubmissionEvaluationsOperationClaims.Admin, SubmissionEvaluationsOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListSubmissionEvaluations({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetSubmissionEvaluations";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListSubmissionEvaluationQueryHandler : IRequestHandler<GetListSubmissionEvaluationQuery, GetListResponse<GetListSubmissionEvaluationListItemDto>>
    {
        private readonly ISubmissionEvaluationRepository _repository; private readonly IMapper _mapper;
        public GetListSubmissionEvaluationQueryHandler(ISubmissionEvaluationRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListSubmissionEvaluationListItemDto>> Handle(GetListSubmissionEvaluationQuery request, CancellationToken cancellationToken)
        {
            IPaginate<SubmissionEvaluation> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListSubmissionEvaluationListItemDto>>(entities);
        }
    }
}
