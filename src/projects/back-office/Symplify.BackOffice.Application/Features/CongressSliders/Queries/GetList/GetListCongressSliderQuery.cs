using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressSliders.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSliders.Queries.GetList;
public class GetListCongressSliderQuery : IRequest<GetListResponse<GetListCongressSliderListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public bool? IsActive { get; set; }
    public string[] Roles => new[] { CongressSlidersOperationClaims.Admin, CongressSlidersOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListCongressSliders({PageRequest.Page},{PageRequest.PageSize},{LanguageId},{Culture},{IsActive})";
    public string CacheGroupKey => "GetCongressSliders";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListCongressSliderQueryHandler : IRequestHandler<GetListCongressSliderQuery, GetListResponse<GetListCongressSliderListItemDto>>
    {
        private readonly ICongressSliderRepository _repository; private readonly ICongressSliderTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly ICurrentLanguageProvider _currentLanguageProvider; private readonly ITranslationFallbackResolver _fallbackResolver;
        public GetListCongressSliderQueryHandler(ICongressSliderRepository repository, ICongressSliderTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, ICurrentLanguageProvider currentLanguageProvider, ITranslationFallbackResolver fallbackResolver) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _currentLanguageProvider = currentLanguageProvider; _fallbackResolver = fallbackResolver; }
        public async Task<GetListResponse<GetListCongressSliderListItemDto>> Handle(GetListCongressSliderQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);
            List<CongressSlider> roots = _repository.Query().ToList();
            if (request.IsActive.HasValue) roots = roots.Where(x => x.GetType().GetProperty("IsActive")?.GetValue(x) is bool b && b == request.IsActive.Value).ToList();
            roots = roots.OrderBy(x => x.GetType().GetProperty("Order")?.GetValue(x) as int? ?? int.MaxValue).ThenBy(x => x.Id).ToList();
            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize;
            int total = roots.Count;
            List<CongressSlider> paged = roots.Skip(page * pageSize).Take(pageSize).ToList();
            HashSet<Guid> ids = paged.Select(x => x.Id).ToHashSet();
            List<CongressSliderTranslation> translations = _translationRepository.Query().ToList().Where(x => ids.Contains(x.CongressSliderId)).ToList();
            List<GetListCongressSliderListItemDto> items = paged.Select(entity =>
            {
                List<CongressSliderTranslation> rootTranslations = translations.Where(x => EqualityComparer<Guid>.Default.Equals(x.CongressSliderId, entity.Id)).ToList();
                CongressSliderTranslation? requestedTranslation = rootTranslations.FirstOrDefault(x => x.LanguageId == requestedLanguage.Id);
                CongressSliderTranslation? displayTranslation = _fallbackResolver.Resolve(rootTranslations, requestedLanguage.Id, defaultLanguage.Id);
                return new GetListCongressSliderListItemDto
                {
                    Id = entity.Id,
                CongressId = entity.CongressId,
                ImagePath = entity.ImagePath,
                Order = entity.Order,
                IsActive = entity.IsActive,
                Title = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Title")!,
                Subtitle = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Subtitle")!,
                ButtonText = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "ButtonText")!,
                ButtonUrl = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "ButtonUrl")!,
                    DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                    IsFallback = requestedTranslation is null && displayTranslation is not null
                };
            }).ToList();
            int pages = (int)Math.Ceiling(total / (double)pageSize);
            return new GetListResponse<GetListCongressSliderListItemDto> { Index = page, Size = pageSize, Count = total, Pages = pages, HasPrevious = page > 0, HasNext = page + 1 < pages, Items = items };
        }
        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(Guid? languageId, string? culture, ApplicationLanguageDto defaultLanguage, CancellationToken cancellationToken)
        {
            if (languageId.HasValue) return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;
            if (!string.IsNullOrWhiteSpace(culture)) return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;
            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }
    }
}
