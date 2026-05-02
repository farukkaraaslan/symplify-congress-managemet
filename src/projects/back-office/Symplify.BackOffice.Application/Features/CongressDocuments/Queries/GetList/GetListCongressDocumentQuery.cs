using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressDocuments.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressDocuments.Queries.GetList;
public class GetListCongressDocumentQuery : IRequest<GetListResponse<GetListCongressDocumentListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { CongressDocumentsOperationClaims.Admin, CongressDocumentsOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListCongressDocuments({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetCongressDocuments";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListCongressDocumentQueryHandler : IRequestHandler<GetListCongressDocumentQuery, GetListResponse<GetListCongressDocumentListItemDto>>
    {
        private readonly ICongressDocumentRepository _repository; private readonly IMapper _mapper;
        public GetListCongressDocumentQueryHandler(ICongressDocumentRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListCongressDocumentListItemDto>> Handle(GetListCongressDocumentQuery request, CancellationToken cancellationToken)
        {
            IPaginate<CongressDocument> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListCongressDocumentListItemDto>>(entities);
        }
    }
}
