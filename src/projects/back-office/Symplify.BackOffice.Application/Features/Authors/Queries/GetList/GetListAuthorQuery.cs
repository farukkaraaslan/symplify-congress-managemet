using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.Authors.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
namespace Symplify.BackOffice.Application.Features.Authors.Queries.GetList;
public class GetListAuthorQuery : IRequest<GetListResponse<GetListAuthorListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { AuthorsOperationClaims.Admin, AuthorsOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListAuthors({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetAuthors";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListAuthorQueryHandler : IRequestHandler<GetListAuthorQuery, GetListResponse<GetListAuthorListItemDto>>
    {
        private readonly IAuthorRepository _repository; private readonly IMapper _mapper;
        public GetListAuthorQueryHandler(IAuthorRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListAuthorListItemDto>> Handle(GetListAuthorQuery request, CancellationToken cancellationToken)
        {
            IPaginate<Author> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListAuthorListItemDto>>(entities);
        }
    }
}
