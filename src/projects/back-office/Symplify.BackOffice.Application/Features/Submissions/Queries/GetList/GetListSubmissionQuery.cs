using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Submissions.Queries.GetList;
public class GetListSubmissionQuery : IRequest<GetListResponse<GetListSubmissionListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { SubmissionsOperationClaims.Admin, SubmissionsOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListSubmissions({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetSubmissions";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListSubmissionQueryHandler : IRequestHandler<GetListSubmissionQuery, GetListResponse<GetListSubmissionListItemDto>>
    {
        private readonly ISubmissionRepository _repository; private readonly IMapper _mapper;
        public GetListSubmissionQueryHandler(ISubmissionRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListSubmissionListItemDto>> Handle(GetListSubmissionQuery request, CancellationToken cancellationToken)
        {
            IPaginate<Submission> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListSubmissionListItemDto>>(entities);
        }
    }
}
