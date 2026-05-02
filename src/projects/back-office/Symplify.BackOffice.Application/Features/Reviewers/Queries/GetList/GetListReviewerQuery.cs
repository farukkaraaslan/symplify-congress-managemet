using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.Reviewers.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Reviewers.Queries.GetList;
public class GetListReviewerQuery : IRequest<GetListResponse<GetListReviewerListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { ReviewersOperationClaims.Admin, ReviewersOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListReviewers({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetReviewers";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListReviewerQueryHandler : IRequestHandler<GetListReviewerQuery, GetListResponse<GetListReviewerListItemDto>>
    {
        private readonly IReviewerRepository _repository; private readonly IMapper _mapper;
        public GetListReviewerQueryHandler(IReviewerRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListReviewerListItemDto>> Handle(GetListReviewerQuery request, CancellationToken cancellationToken)
        {
            IPaginate<Reviewer> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListReviewerListItemDto>>(entities);
        }
    }
}
