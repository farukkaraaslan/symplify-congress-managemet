using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Symplify.BackOffice.Application.Features.Countries.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Domain.Reference.Translations;
namespace Symplify.BackOffice.Application.Features.Countries.Queries.GetList;
public class GetListCountryQuery : IRequest<GetListResponse<GetListCountryListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public bool? IsActive { get; set; }
    public string[] Roles => new[] { CountriesOperationClaims.Admin, CountriesOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListCountries({PageRequest.Page},{PageRequest.PageSize},{LanguageId},{Culture},{IsActive})";
    public string CacheGroupKey => "GetCountries";
    public TimeSpan? SlidingExpiration { get; }
    public class GetListCountryQueryHandler : IRequestHandler<GetListCountryQuery, GetListResponse<GetListCountryListItemDto>>
    {
        private readonly ICountryRepository _repository; private readonly ICountryTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly ICurrentLanguageProvider _currentLanguageProvider; private readonly ITranslationFallbackResolver _fallbackResolver;
        public GetListCountryQueryHandler(ICountryRepository repository, ICountryTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, ICurrentLanguageProvider currentLanguageProvider, ITranslationFallbackResolver fallbackResolver) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _currentLanguageProvider = currentLanguageProvider; _fallbackResolver = fallbackResolver; }
        public async Task<GetListResponse<GetListCountryListItemDto>> Handle(GetListCountryQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);
            List<Country> roots = _repository.Query().ToList();
            if (request.IsActive.HasValue) roots = roots.Where(x => x.GetType().GetProperty("IsActive")?.GetValue(x) is bool b && b == request.IsActive.Value).ToList();
            roots = roots.OrderBy(x => x.GetType().GetProperty("Order")?.GetValue(x) as int? ?? int.MaxValue).ThenBy(x => x.Id).ToList();
            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize;
            int total = roots.Count;
            List<Country> paged = roots.Skip(page * pageSize).Take(pageSize).ToList();
            HashSet<Guid> ids = paged.Select(x => x.Id).ToHashSet();
            List<CountryTranslation> translations = _translationRepository.Query().ToList().Where(x => ids.Contains(x.CountryId)).ToList();
            List<GetListCountryListItemDto> items = paged.Select(entity =>
            {
                List<CountryTranslation> rootTranslations = translations.Where(x => EqualityComparer<Guid>.Default.Equals(x.CountryId, entity.Id)).ToList();
                CountryTranslation? requestedTranslation = rootTranslations.FirstOrDefault(x => x.LanguageId == requestedLanguage.Id);
                CountryTranslation? displayTranslation = _fallbackResolver.Resolve(rootTranslations, requestedLanguage.Id, defaultLanguage.Id);
                return new GetListCountryListItemDto
                {
                    Id = entity.Id,
                Code = entity.Code,
                PhoneCode = entity.PhoneCode,
                IsActive = entity.IsActive,
                Order = entity.Order,
                Name = displayTranslation is null ? string.Empty : (string)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Name")!,
                    DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                    IsFallback = requestedTranslation is null && displayTranslation is not null
                };
            }).ToList();
            int pages = (int)Math.Ceiling(total / (double)pageSize);
            return new GetListResponse<GetListCountryListItemDto> { Index = page, Size = pageSize, Count = total, Pages = pages, HasPrevious = page > 0, HasNext = page + 1 < pages, Items = items };
        }
        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(Guid? languageId, string? culture, ApplicationLanguageDto defaultLanguage, CancellationToken cancellationToken)
        {
            if (languageId.HasValue) return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;
            if (!string.IsNullOrWhiteSpace(culture)) return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;
            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }
    }
}
