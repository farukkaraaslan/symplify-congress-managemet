using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using Core.Application.Storage;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.CongressSliders.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressSliders.Queries.GetList;

public class GetListCongressSliderQuery : IRequest<GetListResponse<GetListCongressSliderListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public Guid CongressId { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchText { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }

    public string[] Roles => new[] { CongressSlidersOperationClaims.Admin, CongressSlidersOperationClaims.Read };

    // Contains presigned image URLs; do not cache this response with a longer-lived cache entry.
    public bool BypassCache => true;
    public string CacheKey => $"GetListCongressSliders({CongressId},{PageRequest.Page},{PageRequest.PageSize},{LanguageId},{Culture},{IsActive},{SearchText},{SortColumn},{SortDirection})";
    public string CacheGroupKey => "GetCongressSliders";
    public TimeSpan? SlidingExpiration { get; }

    public class GetListCongressSliderQueryHandler : IRequestHandler<GetListCongressSliderQuery, GetListResponse<GetListCongressSliderListItemDto>>
    {
        private readonly ICongressSliderRepository _repository;
        private readonly ICongressSliderTranslationRepository _translationRepository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListCongressSliderQueryHandler(
            ICongressSliderRepository repository,
            ICongressSliderTranslationRepository translationRepository,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetListResponse<GetListCongressSliderListItemDto>> Handle(GetListCongressSliderQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);

            List<CongressSlider> roots = _repository.Query()
                .ToList()
                .Where(x => x.CongressId == request.CongressId)
                .ToList();

            if (request.IsActive.HasValue)
                roots = roots.Where(x => x.IsActive == request.IsActive.Value).ToList();

            List<Guid> rootIds = roots.Select(x => x.Id).ToList();
            List<CongressSliderTranslation> allTranslations = _translationRepository.Query()
                .ToList()
                .Where(x => rootIds.Contains(x.CongressSliderId))
                .ToList();

            List<GetListCongressSliderListItemDto> projected = new();

            foreach (CongressSlider entity in roots)
            {
                List<CongressSliderTranslation> rootTranslations = allTranslations
                    .Where(x => x.CongressSliderId == entity.Id)
                    .ToList();

                CongressSliderTranslation? requestedTranslation = rootTranslations.FirstOrDefault(x => x.LanguageId == requestedLanguage.Id);
                CongressSliderTranslation? displayTranslation = _fallbackResolver.Resolve(rootTranslations, requestedLanguage.Id, defaultLanguage.Id);

                projected.Add(new GetListCongressSliderListItemDto
                {
                    Id = entity.Id,
                    CongressId = entity.CongressId,
                    ImagePath = entity.ImagePath,
                    ImagePreviewUrl = await ResolveImagePreviewUrlAsync(entity.ImagePath, cancellationToken),
                    Order = entity.Order,
                    IsActive = entity.IsActive,
                    Title = displayTranslation?.Title,
                    Subtitle = displayTranslation?.Subtitle,
                    ButtonText = displayTranslation?.ButtonText,
                    ButtonUrl = displayTranslation?.ButtonUrl,
                    DisplayLanguageId = displayTranslation?.LanguageId ?? Guid.Empty,
                    IsFallback = requestedTranslation is null && displayTranslation is not null
                });
            }

            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                string searchText = request.SearchText.Trim();
                projected = projected.Where(item =>
                        Contains(item.Title, searchText) ||
                        Contains(item.Subtitle, searchText) ||
                        Contains(item.ButtonText, searchText) ||
                        Contains(item.ButtonUrl, searchText) ||
                        Contains(item.Order.ToString(), searchText))
                    .ToList();
            }

            projected = Sort(projected, request.SortColumn, request.SortDirection).ToList();

            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize;
            int total = projected.Count;
            List<GetListCongressSliderListItemDto> paged = projected.Skip(page * pageSize).Take(pageSize).ToList();
            int pages = (int)Math.Ceiling(total / (double)pageSize);

            return new GetListResponse<GetListCongressSliderListItemDto>
            {
                Index = page,
                Size = pageSize,
                Count = total,
                Pages = pages,
                HasPrevious = page > 0,
                HasNext = page + 1 < pages,
                Items = paged
            };
        }

        private async Task<string?> ResolveImagePreviewUrlAsync(string? imagePath, CancellationToken cancellationToken)
        {
            return await BackOfficeObjectStorageHelper.GetReadUrlOrPathAsync(
                _objectStorageService,
                GetCongressImagesBucketNameOrNull(),
                imagePath,
                TimeSpan.FromMinutes(10),
                cancellationToken);
        }

        private string? GetCongressImagesBucketNameOrNull()
        {
            return string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages)
                ? null
                : _storageOptions.Buckets.CongressImages.Trim();
        }

        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(Guid? languageId, string? culture, ApplicationLanguageDto defaultLanguage, CancellationToken cancellationToken)
        {
            if (languageId.HasValue)
                return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;

            if (!string.IsNullOrWhiteSpace(culture))
                return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;

            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }

        private static bool Contains(string? source, string value)
        {
            return !string.IsNullOrWhiteSpace(source) && source.Contains(value, StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<GetListCongressSliderListItemDto> Sort(
            IEnumerable<GetListCongressSliderListItemDto> source,
            string? sortColumn,
            string? sortDirection)
        {
            bool desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            return (sortColumn ?? "order").ToLowerInvariant() switch
            {
                "title" => desc ? source.OrderByDescending(x => x.Title) : source.OrderBy(x => x.Title),
                "isactive" => desc ? source.OrderByDescending(x => x.IsActive) : source.OrderBy(x => x.IsActive),
                "order" => desc ? source.OrderByDescending(x => x.Order) : source.OrderBy(x => x.Order),
                _ => source.OrderBy(x => x.Order).ThenBy(x => x.Title)
            };
        }
    }
}
