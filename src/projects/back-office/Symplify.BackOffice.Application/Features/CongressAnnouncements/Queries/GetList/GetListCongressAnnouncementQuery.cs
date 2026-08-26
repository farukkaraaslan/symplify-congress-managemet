using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.CongressAnnouncements.Queries.GetList;

public class GetListCongressAnnouncementQuery : IRequest<GetListResponse<GetListCongressAnnouncementListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public Guid CongressId { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public CongressAnnouncementStatus? Status { get; set; }
    public CongressAnnouncementType? Type { get; set; }
    public bool? IsActive { get; set; }
    public bool? ShowOnHomePage { get; set; }
    public bool? ShowInTicker { get; set; }
    public bool? OnlyCurrentlyPublished { get; set; }
    public string? SearchText { get; set; }
    public string? SortColumn { get; set; }
    public string? SortDirection { get; set; }

    public string[] Roles => new[] { CongressAnnouncementsOperationClaims.Admin, CongressAnnouncementsOperationClaims.Read };
    public bool BypassCache { get; }
    public string CacheKey => $"GetListCongressAnnouncements({PageRequest.Page},{PageRequest.PageSize},{CongressId},{LanguageId},{Culture},{Status},{Type},{IsActive},{ShowOnHomePage},{ShowInTicker},{OnlyCurrentlyPublished},{SearchText},{SortColumn},{SortDirection})";
    public string CacheGroupKey => "GetCongressAnnouncements";
    public TimeSpan? SlidingExpiration { get; }

    public class Handler : IRequestHandler<GetListCongressAnnouncementQuery, GetListResponse<GetListCongressAnnouncementListItemDto>>
    {
        private readonly ICongressAnnouncementRepository _repository;
        private readonly ICongressAnnouncementTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public Handler(ICongressAnnouncementRepository repository, ICongressAnnouncementTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, ICurrentLanguageProvider currentLanguageProvider, ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetListResponse<GetListCongressAnnouncementListItemDto>> Handle(GetListCongressAnnouncementQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);
            DateTime utcNow = DateTime.UtcNow;

            List<CongressAnnouncement> roots = _repository.Query().Where(item => item.CongressId == request.CongressId).ToList();
            List<CongressAnnouncementTranslation> allTranslations = _translationRepository.Query().Where(translation => roots.Select(root => root.Id).Contains(translation.CongressAnnouncementId)).ToList();

            roots = ApplyFilters(roots, allTranslations, request, utcNow);
            roots = ApplySorting(roots, allTranslations, requestedLanguage.Id, defaultLanguage.Id, request.SortColumn, request.SortDirection);

            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize;
            int total = roots.Count;
            List<CongressAnnouncement> paged = roots.Skip(page * pageSize).Take(pageSize).ToList();
            HashSet<Guid> ids = paged.Select(root => root.Id).ToHashSet();
            List<CongressAnnouncementTranslation> translations = allTranslations.Where(translation => ids.Contains(translation.CongressAnnouncementId)).ToList();

            List<GetListCongressAnnouncementListItemDto> items = paged.Select(entity =>
            {
                List<CongressAnnouncementTranslation> rootTranslations = translations.Where(translation => translation.CongressAnnouncementId == entity.Id).ToList();
                CongressAnnouncementTranslation? requestedTranslation = rootTranslations.FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);
                CongressAnnouncementTranslation? displayTranslation = _fallbackResolver.Resolve(rootTranslations, requestedLanguage.Id, defaultLanguage.Id);
                return Project(entity, displayTranslation, requestedTranslation is null && displayTranslation is not null, utcNow);
            }).ToList();

            int pages = (int)Math.Ceiling(total / (double)pageSize);
            return new GetListResponse<GetListCongressAnnouncementListItemDto>
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

        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(Guid? languageId, string? culture, ApplicationLanguageDto defaultLanguage, CancellationToken cancellationToken)
        {
            if (languageId.HasValue) return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;
            if (!string.IsNullOrWhiteSpace(culture)) return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;
            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }

        private static List<CongressAnnouncement> ApplyFilters(List<CongressAnnouncement> roots, List<CongressAnnouncementTranslation> translations, GetListCongressAnnouncementQuery request, DateTime utcNow)
        {
            if (request.Status.HasValue) roots = roots.Where(item => item.Status == request.Status.Value).ToList();
            if (request.Type.HasValue) roots = roots.Where(item => item.Type == request.Type.Value).ToList();
            if (request.IsActive.HasValue) roots = roots.Where(item => item.IsActive == request.IsActive.Value).ToList();
            if (request.ShowOnHomePage.HasValue) roots = roots.Where(item => item.ShowOnHomePage == request.ShowOnHomePage.Value).ToList();
            if (request.ShowInTicker.HasValue) roots = roots.Where(item => item.ShowInTicker == request.ShowInTicker.Value).ToList();
            if (request.OnlyCurrentlyPublished == true) roots = roots.Where(item => IsCurrentlyPublished(item, utcNow)).ToList();

            if (string.IsNullOrWhiteSpace(request.SearchText)) return roots;

            string search = request.SearchText.Trim().ToLowerInvariant();
            HashSet<Guid> matchingIds = translations
                .Where(translation =>
                    (!string.IsNullOrWhiteSpace(translation.Title) && translation.Title.ToLower().Contains(search)) ||
                    (!string.IsNullOrWhiteSpace(translation.Summary) && translation.Summary.ToLower().Contains(search)) ||
                    (!string.IsNullOrWhiteSpace(translation.Content) && translation.Content.ToLower().Contains(search)))
                .Select(translation => translation.CongressAnnouncementId)
                .ToHashSet();

            return roots.Where(item => matchingIds.Contains(item.Id) || (item.ExternalUrl != null && item.ExternalUrl.ToLower().Contains(search))).ToList();
        }

        private static List<CongressAnnouncement> ApplySorting(List<CongressAnnouncement> roots, List<CongressAnnouncementTranslation> translations, Guid requestedLanguageId, Guid defaultLanguageId, string? sortColumn, string? sortDirection)
        {
            bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            string column = string.IsNullOrWhiteSpace(sortColumn) ? "order" : sortColumn.Trim().ToLowerInvariant();

            string TitleFor(CongressAnnouncement item)
            {
                CongressAnnouncementTranslation? requested = translations.FirstOrDefault(translation => translation.CongressAnnouncementId == item.Id && translation.LanguageId == requestedLanguageId);
                CongressAnnouncementTranslation? fallback = requested ?? translations.FirstOrDefault(translation => translation.CongressAnnouncementId == item.Id && translation.LanguageId == defaultLanguageId);
                return fallback?.Title ?? string.Empty;
            }

            IOrderedEnumerable<CongressAnnouncement> ordered = column switch
            {
                "title" => descending ? roots.OrderByDescending(TitleFor) : roots.OrderBy(TitleFor),
                "type" => descending ? roots.OrderByDescending(item => item.Type) : roots.OrderBy(item => item.Type),
                "status" => descending ? roots.OrderByDescending(item => item.Status) : roots.OrderBy(item => item.Status),
                "publishstartdate" => descending ? roots.OrderByDescending(item => item.PublishStartDate) : roots.OrderBy(item => item.PublishStartDate),
                "publishenddate" => descending ? roots.OrderByDescending(item => item.PublishEndDate) : roots.OrderBy(item => item.PublishEndDate),
                "order" => descending
                    ? roots.OrderByDescending(item => item.Order <= 0 ? int.MinValue : item.Order)
                    : roots.OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order),

                _ => descending
                    ? roots.OrderByDescending(item => item.Order <= 0 ? int.MinValue : item.Order)
                    : roots.OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
            };

            return ordered
                .ThenBy(item => item.Id)
                .ToList();
        }

        private static GetListCongressAnnouncementListItemDto Project(CongressAnnouncement entity, CongressAnnouncementTranslation? translation, bool isFallback, DateTime utcNow)
        {
            return new GetListCongressAnnouncementListItemDto
            {
                Id = entity.Id,
                CongressId = entity.CongressId,
                Type = entity.Type,
                Status = entity.Status,
                PublishStartDate = entity.PublishStartDate,
                PublishEndDate = entity.PublishEndDate,
                IsPinned = entity.IsPinned,
                ShowOnHomePage = entity.ShowOnHomePage,
                ShowInTicker = entity.ShowInTicker,
                ExternalUrl = entity.ExternalUrl,
                AttachmentPath = entity.AttachmentPath,
                Order = entity.Order,
                IsActive = entity.IsActive,
                IsCurrentlyPublished = IsCurrentlyPublished(entity, utcNow),
                Title = translation?.Title ?? string.Empty,
                Summary = translation?.Summary,
                Content = translation?.Content,
                SeoTitle = translation?.SeoTitle,
                SeoDescription = translation?.SeoDescription,
                DisplayLanguageId = translation?.LanguageId ?? default,
                IsFallback = isFallback
            };
        }

        private static bool IsCurrentlyPublished(CongressAnnouncement entity, DateTime utcNow)
            => entity.IsActive &&
               entity.Status == CongressAnnouncementStatus.Published &&
               (!entity.PublishStartDate.HasValue || entity.PublishStartDate.Value <= utcNow) &&
               (!entity.PublishEndDate.HasValue || entity.PublishEndDate.Value >= utcNow);
    }
}
