using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Persistence.Paging;
using MediatR;
using Symplify.BackOffice.Application.Features.Languages.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.Languages.Queries.GetList;
public class GetListLanguageQuery : IRequest<GetListResponse<GetListLanguageListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public string[] Roles => new[] { LanguagesOperationClaims.Admin, LanguagesOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListLanguages({PageRequest.Page},{PageRequest.PageSize})";
    public string CacheGroupKey => "GetLanguages";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListLanguageQueryHandler : IRequestHandler<GetListLanguageQuery, GetListResponse<GetListLanguageListItemDto>>
    {
        private readonly ILanguageRepository _repository; private readonly IMapper _mapper;
        public GetListLanguageQueryHandler(ILanguageRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<GetListResponse<GetListLanguageListItemDto>> Handle(GetListLanguageQuery request, CancellationToken cancellationToken)
        {
            IPaginate<Language> entities = await _repository.GetListAsync(index: request.PageRequest.Page, size: request.PageRequest.PageSize, cancellationToken: cancellationToken);
            return _mapper.Map<GetListResponse<GetListLanguageListItemDto>>(entities);
        }
    }
}
