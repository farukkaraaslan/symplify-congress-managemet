using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Requests;
using Core.Application.Responses;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Queries.GetList;

public class GetListCongressPaymentPlanQuery : IRequest<GetListResponse<GetListCongressPaymentPlanListItemDto>>, ISecuredRequest, ICachableRequest
{
    public PageRequest PageRequest { get; set; } = new();
    public Guid? CongressId { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public string? AudienceType { get; set; }
    public string? PaymentCategory { get; set; }
    public bool? IsPublicVisible { get; set; }
    public bool? IsActive { get; set; }
    public bool OnlyCurrentlyValid { get; set; }
    public string[] Roles => new[] { CongressPaymentPlansOperationClaims.Admin, CongressPaymentPlansOperationClaims.Read };
    public bool BypassCache { get; set; }
    public string CacheKey => $"GetListCongressPaymentPlans({CongressId},{PageRequest.Page},{PageRequest.PageSize},{LanguageId},{Culture},{AudienceType},{PaymentCategory},{IsPublicVisible},{IsActive},{OnlyCurrentlyValid})";
    public string CacheGroupKey => "GetCongressPaymentPlans";
    public TimeSpan? SlidingExpiration { get; }

    public class GetListCongressPaymentPlanQueryHandler : IRequestHandler<GetListCongressPaymentPlanQuery, GetListResponse<GetListCongressPaymentPlanListItemDto>>
    {
        private readonly ICongressPaymentPlanRepository _repository;
        private readonly ICongressPaymentPlanTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetListCongressPaymentPlanQueryHandler(
            ICongressPaymentPlanRepository repository,
            ICongressPaymentPlanTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetListResponse<GetListCongressPaymentPlanListItemDto>> Handle(GetListCongressPaymentPlanQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);

            DateTime now = DateTime.UtcNow;

            List<CongressPaymentPlan> roots = _repository.Query()
                .ToList()
                .Where(entity => !IsDeleted(entity))
                .ToList();

            if (request.CongressId.HasValue && request.CongressId.Value != Guid.Empty)
                roots = roots.Where(entity => entity.CongressId == request.CongressId.Value).ToList();

            if (request.IsActive.HasValue)
                roots = roots.Where(entity => entity.IsActive == request.IsActive.Value).ToList();

            if (request.IsPublicVisible.HasValue)
                roots = roots.Where(entity => entity.IsPublicVisible == request.IsPublicVisible.Value).ToList();

            if (!string.IsNullOrWhiteSpace(request.AudienceType))
            {
                string normalizedAudienceType = CongressPaymentPlanAudienceTypes.Normalize(request.AudienceType);
                roots = roots.Where(entity =>
                    string.Equals(entity.AudienceType, normalizedAudienceType, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entity.AudienceType, CongressPaymentPlanAudienceTypes.All, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.PaymentCategory))
            {
                string normalizedPaymentCategory = CongressPaymentPlanCategories.Normalize(request.PaymentCategory);
                roots = roots.Where(entity => string.Equals(entity.PaymentCategory, normalizedPaymentCategory, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (request.OnlyCurrentlyValid)
            {
                roots = roots.Where(entity =>
                    (!entity.ValidFrom.HasValue || entity.ValidFrom.Value <= now) &&
                    (!entity.ValidUntil.HasValue || entity.ValidUntil.Value >= now))
                    .ToList();
            }

            roots = roots
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            int page = request.PageRequest.Page < 0 ? 0 : request.PageRequest.Page;
            int pageSize = request.PageRequest.PageSize <= 0 ? 20 : request.PageRequest.PageSize;
            int total = roots.Count;

            List<CongressPaymentPlan> paged = roots
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToList();

            HashSet<Guid> ids = paged.Select(entity => entity.Id).ToHashSet();
            List<CongressPaymentPlanTranslation> translations = _translationRepository.Query()
                .ToList()
                .Where(translation => ids.Contains(translation.CongressPaymentPlanId) && !IsDeleted(translation))
                .ToList();

            List<GetListCongressPaymentPlanListItemDto> items = paged.Select(entity =>
            {
                List<CongressPaymentPlanTranslation> rootTranslations = translations
                    .Where(translation => translation.CongressPaymentPlanId == entity.Id)
                    .ToList();

                CongressPaymentPlanTranslation? requestedTranslation = rootTranslations.FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);
                CongressPaymentPlanTranslation? displayTranslation = _fallbackResolver.Resolve(rootTranslations, requestedLanguage.Id, defaultLanguage.Id);

                return new GetListCongressPaymentPlanListItemDto
                {
                    Id = entity.Id,
                    CongressId = entity.CongressId,
                    Code = entity.Code,
                    Amount = entity.Amount,
                    Currency = entity.Currency,
                    AudienceType = entity.AudienceType,
                    PaymentCategory = entity.PaymentCategory,
                    DueDate = entity.DueDate,
                    ValidFrom = entity.ValidFrom,
                    ValidUntil = entity.ValidUntil,
                    Order = entity.Order,
                    IsPublicVisible = entity.IsPublicVisible,
                    IsActive = entity.IsActive,
                    Name = displayTranslation is null ? string.Empty : (string)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Name")!,
                    Description = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Description"),
                    DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                    IsFallback = requestedTranslation is null && displayTranslation is not null
                };
            }).ToList();

            int pages = (int)Math.Ceiling(total / (double)pageSize);

            return new GetListResponse<GetListCongressPaymentPlanListItemDto>
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
            if (languageId.HasValue)
                return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;

            if (!string.IsNullOrWhiteSpace(culture))
                return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;

            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}
