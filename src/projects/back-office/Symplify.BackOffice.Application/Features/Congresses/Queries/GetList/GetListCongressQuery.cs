using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Symplify.BackOffice.Application.Features.Congresses.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.Congresses.Queries.GetList;
public class GetListCongressQuery : IRequest<GetListResponse<GetListCongressListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public bool? IsActive { get; set; }
    public string[] Roles => new[] { CongressesOperationClaims.Admin, CongressesOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListCongresses({PageRequest.Page},{PageRequest.PageSize},{LanguageId},{Culture},{IsActive})";
    public string CacheGroupKey => "GetCongresses";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListCongressQueryHandler : IRequestHandler<GetListCongressQuery, GetListResponse<GetListCongressListItemDto>>
    {
        private readonly ICongressRepository _repository; private readonly ICongressTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly ICurrentLanguageProvider _currentLanguageProvider; private readonly ITranslationFallbackResolver _fallbackResolver;
        public GetListCongressQueryHandler(ICongressRepository repository, ICongressTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, ICurrentLanguageProvider currentLanguageProvider, ITranslationFallbackResolver fallbackResolver) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _currentLanguageProvider = currentLanguageProvider; _fallbackResolver = fallbackResolver; }
        public async Task<GetListResponse<GetListCongressListItemDto>> Handle(GetListCongressQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);
            List<Congress> roots = _repository.Query().ToList();
            if (request.IsActive.HasValue) roots = roots.Where(x => x.GetType().GetProperty("IsActive")?.GetValue(x) is bool b && b == request.IsActive.Value).ToList();
            roots = roots.OrderBy(x => x.GetType().GetProperty("Order")?.GetValue(x) as int? ?? int.MaxValue).ThenBy(x => x.Id).ToList();
            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize;
            int total = roots.Count;
            List<Congress> paged = roots.Skip(page * pageSize).Take(pageSize).ToList();
            HashSet<Guid> ids = paged.Select(x => x.Id).ToHashSet();
            List<CongressTranslation> translations = _translationRepository.Query().ToList().Where(x => ids.Contains(x.CongressId)).ToList();
            List<GetListCongressListItemDto> items = paged.Select(entity =>
            {
                List<CongressTranslation> rootTranslations = translations.Where(x => EqualityComparer<Guid>.Default.Equals(x.CongressId, entity.Id)).ToList();
                CongressTranslation? requestedTranslation = rootTranslations.FirstOrDefault(x => x.LanguageId == requestedLanguage.Id);
                CongressTranslation? displayTranslation = _fallbackResolver.Resolve(rootTranslations, requestedLanguage.Id, defaultLanguage.Id);
                return new GetListCongressListItemDto
                {
                    Id = entity.Id,
                TenantId = entity.TenantId,
                Code = entity.Code,
                Name = entity.Name,
                Slug = entity.Slug,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Status = entity.Status,
                IsActive = entity.IsActive,
                Title = displayTranslation is null ? string.Empty : (string)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Title")!,
                Subtitle = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Subtitle")!,
                Description = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Description")!,
                LogoPath = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "LogoPath")!,
                    DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                    IsFallback = requestedTranslation is null && displayTranslation is not null
                };
            }).ToList();
            int pages = (int)Math.Ceiling(total / (double)pageSize);
            return new GetListResponse<GetListCongressListItemDto> { Index = page, Size = pageSize, Count = total, Pages = pages, HasPrevious = page > 0, HasNext = page + 1 < pages, Items = items };
        }
        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(Guid? languageId, string? culture, ApplicationLanguageDto defaultLanguage, CancellationToken cancellationToken)
        {
            if (languageId.HasValue) return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;
            if (!string.IsNullOrWhiteSpace(culture)) return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;
            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }
    }
}
