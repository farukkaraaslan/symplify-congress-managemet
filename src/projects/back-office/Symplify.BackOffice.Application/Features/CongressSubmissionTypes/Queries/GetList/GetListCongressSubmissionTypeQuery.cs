using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Queries.GetList;
public class GetListCongressSubmissionTypeQuery : IRequest<GetListResponse<GetListCongressSubmissionTypeListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { CongressSubmissionTypesOperationClaims.Admin, CongressSubmissionTypesOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListCongressSubmissionTypes({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetCongressSubmissionTypes";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListCongressSubmissionTypeQueryHandler : IRequestHandler<GetListCongressSubmissionTypeQuery, GetListResponse<GetListCongressSubmissionTypeListItemDto>>
    {
        private readonly ICongressSubmissionTypeRepository _repository; private readonly IMapper _mapper;
        public GetListCongressSubmissionTypeQueryHandler(ICongressSubmissionTypeRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListCongressSubmissionTypeListItemDto>> Handle(GetListCongressSubmissionTypeQuery request, CancellationToken cancellationToken)
        {
            IPaginate<CongressSubmissionType> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListCongressSubmissionTypeListItemDto>>(entities);
        }
    }
}
