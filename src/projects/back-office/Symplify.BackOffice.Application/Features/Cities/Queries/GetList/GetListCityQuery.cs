using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Symplify.BackOffice.Application.Features.Cities.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Domain.Reference.Translations;
namespace Symplify.BackOffice.Application.Features.Cities.Queries.GetList;
public class GetListCityQuery : IRequest<GetListResponse<GetListCityListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public bool? IsActive { get; set; }
    public string[] Roles => new[] { CitiesOperationClaims.Admin, CitiesOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListCities({PageRequest.Page},{PageRequest.PageSize},{LanguageId},{Culture},{IsActive})";
    public string CacheGroupKey => "GetCities";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListCityQueryHandler : IRequestHandler<GetListCityQuery, GetListResponse<GetListCityListItemDto>>
    {
        private readonly ICityRepository _repository; private readonly ICityTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly ICurrentLanguageProvider _currentLanguageProvider; private readonly ITranslationFallbackResolver _fallbackResolver;
        public GetListCityQueryHandler(ICityRepository repository, ICityTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, ICurrentLanguageProvider currentLanguageProvider, ITranslationFallbackResolver fallbackResolver) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _currentLanguageProvider = currentLanguageProvider; _fallbackResolver = fallbackResolver; }
        public async Task<GetListResponse<GetListCityListItemDto>> Handle(GetListCityQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);
            List<City> roots = _repository.Query().ToList();
            if (request.IsActive.HasValue) roots = roots.Where(x => x.GetType().GetProperty("IsActive")?.GetValue(x) is bool b && b == request.IsActive.Value).ToList();
            roots = roots.OrderBy(x => x.GetType().GetProperty("Order")?.GetValue(x) as int? ?? int.MaxValue).ThenBy(x => x.Id).ToList();
            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize;
            int total = roots.Count;
            List<City> paged = roots.Skip(page * pageSize).Take(pageSize).ToList();
            HashSet<Guid> ids = paged.Select(x => x.Id).ToHashSet();
            List<CityTranslation> translations = _translationRepository.Query().ToList().Where(x => ids.Contains(x.CityId)).ToList();
            List<GetListCityListItemDto> items = paged.Select(entity =>
            {
                List<CityTranslation> rootTranslations = translations.Where(x => EqualityComparer<Guid>.Default.Equals(x.CityId, entity.Id)).ToList();
                CityTranslation? requestedTranslation = rootTranslations.FirstOrDefault(x => x.LanguageId == requestedLanguage.Id);
                CityTranslation? displayTranslation = _fallbackResolver.Resolve(rootTranslations, requestedLanguage.Id, defaultLanguage.Id);
                return new GetListCityListItemDto
                {
                    Id = entity.Id,
                StateId = entity.StateId,
                IsActive = entity.IsActive,
                Order = entity.Order,
                Name = displayTranslation is null ? string.Empty : (string)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Name")!,
                    DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                    IsFallback = requestedTranslation is null && displayTranslation is not null
                };
            }).ToList();
            int pages = (int)Math.Ceiling(total / (double)pageSize);
            return new GetListResponse<GetListCityListItemDto> { Index = page, Size = pageSize, Count = total, Pages = pages, HasPrevious = page > 0, HasNext = page + 1 < pages, Items = items };
        }
        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(Guid? languageId, string? culture, ApplicationLanguageDto defaultLanguage, CancellationToken cancellationToken)
        {
            if (languageId.HasValue) return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;
            if (!string.IsNullOrWhiteSpace(culture)) return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;
            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }
    }
}
