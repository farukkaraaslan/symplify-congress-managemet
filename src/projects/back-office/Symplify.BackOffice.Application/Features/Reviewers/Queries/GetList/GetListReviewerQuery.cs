using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
        private readonly IReviewerRepository _repository;

        public GetListReviewerQueryHandler(IReviewerRepository repository)
        {
            _repository = repository;
        }

        public async Task<GetListResponse<GetListReviewerListItemDto>> Handle(GetListReviewerQuery request, CancellationToken cancellationToken)
        {
            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 50 : request.PageRequest.PageSize;

            IQueryable<Reviewer> query = _repository
                .Query()
                .AsNoTracking()
                .Include(reviewer => reviewer.User)
                .Where(reviewer => reviewer.DeletedDate == null)
                .OrderBy(reviewer => reviewer.User.Name)
                .ThenBy(reviewer => reviewer.User.Surname);

            int total = await query.CountAsync(cancellationToken);
            int pages = pageSize == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            List<GetListReviewerListItemDto> items = await query
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(reviewer => new GetListReviewerListItemDto
                {
                    Id = reviewer.Id,
                    UserId = reviewer.UserId,
                    FullName = (reviewer.User.Name + " " + reviewer.User.Surname).Trim(),
                    Email = reviewer.User.Email,
                    Institution = reviewer.User.Institution,
                    Orcid = reviewer.User.Orcid,
                    Status = reviewer.Status,
                    IsActive = reviewer.IsActive,
                    CreatedDate = reviewer.CreatedDate
                })
                .ToListAsync(cancellationToken);

            return new GetListResponse<GetListReviewerListItemDto>
            {
                Index = page,
                Size = pageSize,
                Count = total,
                Pages = pages,
                HasPrevious = page > 0,
                HasNext = page + 1 < pages,
                Items = items
            };
        }
    }
}
