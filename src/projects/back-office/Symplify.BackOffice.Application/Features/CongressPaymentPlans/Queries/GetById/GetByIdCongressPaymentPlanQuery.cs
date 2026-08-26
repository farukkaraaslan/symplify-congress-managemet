using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Queries.GetById;

public class GetByIdCongressPaymentPlanQuery : IRequest<GetByIdCongressPaymentPlanResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public string[] Roles => new[] { CongressPaymentPlansOperationClaims.Admin, CongressPaymentPlansOperationClaims.Read };

    public class GetByIdCongressPaymentPlanQueryHandler : IRequestHandler<GetByIdCongressPaymentPlanQuery, GetByIdCongressPaymentPlanResponse>
    {
        private readonly ICongressPaymentPlanRepository _repository;
        private readonly ICongressPaymentPlanTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetByIdCongressPaymentPlanQueryHandler(
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

        public async Task<GetByIdCongressPaymentPlanResponse> Handle(GetByIdCongressPaymentPlanQuery request, CancellationToken cancellationToken)
        {
            CongressPaymentPlan? entity = await _repository.GetAsync(predicate: x => x.Id.Equals(request.Id));

            if (entity is null)
                throw new BusinessException(CongressPaymentPlansMessages.EntityNotFound);

            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);

            List<CongressPaymentPlanTranslation> translations = _translationRepository.Query()
                .ToList()
                .Where(x => x.CongressPaymentPlanId == request.Id && !IsDeleted(x))
                .ToList();

            CongressPaymentPlanTranslation? requestedTranslation = translations.FirstOrDefault(x => x.LanguageId == requestedLanguage.Id);
            CongressPaymentPlanTranslation? displayTranslation = _fallbackResolver.Resolve(translations, requestedLanguage.Id, defaultLanguage.Id);

            return new GetByIdCongressPaymentPlanResponse
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
