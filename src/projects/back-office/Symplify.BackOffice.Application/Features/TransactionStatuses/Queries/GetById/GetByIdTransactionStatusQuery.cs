using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Queries.GetById;

public class GetByIdTransactionStatusQuery : IRequest<GetByIdTransactionStatusResponse>, ISecuredRequest
{
    public int Id { get; set; }

    public Guid? LanguageId { get; set; }

    public string? Culture { get; set; }

    public string[] Roles => new[] { TransactionStatusesOperationClaims.Admin, TransactionStatusesOperationClaims.Read };

    public class GetByIdTransactionStatusQueryHandler
        : IRequestHandler<GetByIdTransactionStatusQuery, GetByIdTransactionStatusResponse>
    {
        private readonly ITransactionStatusRepository _repository;
        private readonly ITransactionStatusTranslationRepository _translationRepository;
        private readonly ITransactionStatusPhaseRepository _phaseRepository;
        private readonly ITransactionStatusPhaseTranslationRepository _phaseTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetByIdTransactionStatusQueryHandler(
            ITransactionStatusRepository repository,
            ITransactionStatusTranslationRepository translationRepository,
            ITransactionStatusPhaseRepository phaseRepository,
            ITransactionStatusPhaseTranslationRepository phaseTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _phaseRepository = phaseRepository;
            _phaseTranslationRepository = phaseTranslationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetByIdTransactionStatusResponse> Handle(
            GetByIdTransactionStatusQuery request,
            CancellationToken cancellationToken)
        {
            TransactionStatus? entity = await _repository.GetAsync(predicate: root => root.Id.Equals(request.Id));

            if (entity is null)
                throw new BusinessException(TransactionStatusesMessages.EntityNotFound);

            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(
                request.LanguageId,
                request.Culture,
                defaultLanguage,
                cancellationToken);

            List<TransactionStatusTranslation> translations = _translationRepository.Query()
                .ToList()
                .Where(translation => translation.TransactionStatusId == request.Id)
                .ToList();

            TransactionStatusTranslation? requestedTranslation = translations.FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);
            TransactionStatusTranslation? displayTranslation = _fallbackResolver.Resolve(translations, requestedLanguage.Id, defaultLanguage.Id);

            TransactionStatusPhase? phase = await _phaseRepository.GetAsync(predicate: item => item.Id == entity.TransactionStatusPhaseId);
            List<TransactionStatusPhaseTranslation> phaseTranslations = _phaseTranslationRepository.Query()
                .ToList()
                .Where(translation => translation.TransactionStatusPhaseId == entity.TransactionStatusPhaseId)
                .ToList();

            TransactionStatusPhaseTranslation? displayPhaseTranslation = _fallbackResolver.Resolve(
                phaseTranslations,
                requestedLanguage.Id,
                defaultLanguage.Id);

            return new GetByIdTransactionStatusResponse
            {
                Id = entity.Id,
                TransactionStatusPhaseId = entity.TransactionStatusPhaseId,
                TransactionStatusPhaseCode = phase?.Code ?? string.Empty,
                TransactionStatusPhaseName = displayPhaseTranslation is null
                    ? string.Empty
                    : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayPhaseTranslation, "Name") ?? string.Empty,
                Code = entity.Code,
                Order = entity.Order,
                IsEditable = entity.IsEditable,
                IsFinal = entity.IsFinal,
                IsActive = entity.IsActive,
                Name = displayTranslation is null
                    ? string.Empty
                    : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Name") ?? string.Empty,
                Description = displayTranslation is null
                    ? null
                    : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Description"),
                DisplayLanguageId = displayTranslation?.LanguageId ?? default,
                IsFallback = requestedTranslation is null && displayTranslation is not null
            };
        }

        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(
            Guid? languageId,
            string? culture,
            ApplicationLanguageDto defaultLanguage,
            CancellationToken cancellationToken)
        {
            if (languageId.HasValue)
                return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;

            if (!string.IsNullOrWhiteSpace(culture))
                return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;

            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }
    }
}
