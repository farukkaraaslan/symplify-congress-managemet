using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Queries.GetPublicList;

/// <summary>
/// Public site ve submission ödeme bilgilendirme ekranı için kullanılır.
/// Secured request değildir; sadece aktif, public ve geçerli planları döner.
/// </summary>
public class GetPublicCongressPaymentPlansQuery : IRequest<IReadOnlyList<GetPublicCongressPaymentPlanListItemDto>>
{
    public Guid CongressId { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }

    /// <summary>
    /// All, Domestic, International. Boşsa sadece All filtrelenmez; tüm public planlar döner.
    /// Submission tarafında User.Country bazlı Domestic/International gönderilmelidir.
    /// </summary>
    public string? AudienceType { get; set; }

    public class GetPublicCongressPaymentPlansQueryHandler : IRequestHandler<GetPublicCongressPaymentPlansQuery, IReadOnlyList<GetPublicCongressPaymentPlanListItemDto>>
    {
        private readonly ICongressPaymentPlanRepository _repository;
        private readonly ICongressPaymentPlanTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetPublicCongressPaymentPlansQueryHandler(
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

        public async Task<IReadOnlyList<GetPublicCongressPaymentPlanListItemDto>> Handle(GetPublicCongressPaymentPlansQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);
            DateTime now = DateTime.UtcNow;

            List<CongressPaymentPlan> roots = _repository.Query()
                .ToList()
                .Where(entity =>
                    entity.CongressId == request.CongressId &&
                    entity.IsActive &&
                    entity.IsPublicVisible &&
                    !IsDeleted(entity) &&
                    (!entity.ValidFrom.HasValue || entity.ValidFrom.Value <= now) &&
                    (!entity.ValidUntil.HasValue || entity.ValidUntil.Value >= now))
                .ToList();

            if (!string.IsNullOrWhiteSpace(request.AudienceType))
            {
                string normalizedAudienceType = CongressPaymentPlanAudienceTypes.Normalize(request.AudienceType);

                roots = roots
                    .Where(entity =>
                        string.Equals(entity.AudienceType, CongressPaymentPlanAudienceTypes.All, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(entity.AudienceType, normalizedAudienceType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            roots = roots
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            HashSet<Guid> ids = roots.Select(entity => entity.Id).ToHashSet();
            List<CongressPaymentPlanTranslation> translations = _translationRepository.Query()
                .ToList()
                .Where(translation => ids.Contains(translation.CongressPaymentPlanId) && !IsDeleted(translation))
                .ToList();

            return roots.Select(entity =>
            {
                List<CongressPaymentPlanTranslation> rootTranslations = translations
                    .Where(translation => translation.CongressPaymentPlanId == entity.Id)
                    .ToList();

                CongressPaymentPlanTranslation? requestedTranslation = rootTranslations.FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);
                CongressPaymentPlanTranslation? displayTranslation = _fallbackResolver.Resolve(rootTranslations, requestedLanguage.Id, defaultLanguage.Id);

                return new GetPublicCongressPaymentPlanListItemDto
                {
                    Id = entity.Id,
                    Code = entity.Code,
                    Amount = entity.Amount,
                    Currency = entity.Currency,
                    AudienceType = entity.AudienceType,
                    PaymentCategory = entity.PaymentCategory,
                    DueDate = entity.DueDate,
                    ValidFrom = entity.ValidFrom,
                    ValidUntil = entity.ValidUntil,
                    Order = entity.Order,
                    Name = displayTranslation is null ? string.Empty : (string)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Name")!,
                    Description = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Description"),
                    DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                    IsFallback = requestedTranslation is null && displayTranslation is not null
                };
            }).ToList();
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
