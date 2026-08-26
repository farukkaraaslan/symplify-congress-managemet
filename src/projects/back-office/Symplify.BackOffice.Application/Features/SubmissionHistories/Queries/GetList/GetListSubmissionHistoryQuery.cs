using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.SubmissionHistories.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.SubmissionHistories.Queries.GetList;
public class GetListSubmissionHistoryQuery : IRequest<GetListResponse<GetListSubmissionHistoryListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { SubmissionHistoriesOperationClaims.Admin, SubmissionHistoriesOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListSubmissionHistories({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetSubmissionHistories";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListSubmissionHistoryQueryHandler : IRequestHandler<GetListSubmissionHistoryQuery, GetListResponse<GetListSubmissionHistoryListItemDto>>
    {
        private readonly ISubmissionHistoryRepository _repository; private readonly IMapper _mapper;
        public GetListSubmissionHistoryQueryHandler(ISubmissionHistoryRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListSubmissionHistoryListItemDto>> Handle(GetListSubmissionHistoryQuery request, CancellationToken cancellationToken)
        {
            IPaginate<SubmissionHistory> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListSubmissionHistoryListItemDto>>(entities);
        }
    }
}
