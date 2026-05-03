using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Queries.GetById;

public class GetByIdTransactionStatusTransitionQuery : IRequest<GetByIdTransactionStatusTransitionResponse>, ISecuredRequest
{
    public int Id { get; set; }

    public Guid? LanguageId { get; set; }

    public string? Culture { get; set; }

    public string[] Roles => new[] { TransactionStatusTransitionsOperationClaims.Admin, TransactionStatusTransitionsOperationClaims.Read };

    public class GetByIdTransactionStatusTransitionQueryHandler
        : IRequestHandler<GetByIdTransactionStatusTransitionQuery, GetByIdTransactionStatusTransitionResponse>
    {
        private readonly ITransactionStatusTransitionRepository _repository;
        private readonly ITransactionStatusTransitionTranslationRepository _translationRepository;
        private readonly ITransactionStatusRepository _transactionStatusRepository;
        private readonly ITransactionStatusTranslationRepository _transactionStatusTranslationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetByIdTransactionStatusTransitionQueryHandler(
            ITransactionStatusTransitionRepository repository,
            ITransactionStatusTransitionTranslationRepository translationRepository,
            ITransactionStatusRepository transactionStatusRepository,
            ITransactionStatusTranslationRepository transactionStatusTranslationRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _transactionStatusRepository = transactionStatusRepository;
            _transactionStatusTranslationRepository = transactionStatusTranslationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetByIdTransactionStatusTransitionResponse> Handle(
            GetByIdTransactionStatusTransitionQuery request,
            CancellationToken cancellationToken)
        {
            TransactionStatusTransition? entity = await _repository.GetAsync(predicate: root => root.Id.Equals(request.Id));

            if (entity is null)
                throw new BusinessException(TransactionStatusTransitionsMessages.EntityNotFound);

            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(
                request.LanguageId,
                request.Culture,
                defaultLanguage,
                cancellationToken);

            List<TransactionStatusTransitionTranslation> translations = _translationRepository.Query()
                .ToList()
                .Where(translation => translation.TransactionStatusTransitionId == request.Id)
                .ToList();

            TransactionStatusTransitionTranslation? requestedTranslation = translations
                .FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);

            TransactionStatusTransitionTranslation? displayTranslation = _fallbackResolver.Resolve(
                translations,
                requestedLanguage.Id,
                defaultLanguage.Id);

            List<TransactionStatus> statuses = _transactionStatusRepository.Query()
                .ToList()
                .Where(status => status.Id == entity.FromStatusId || status.Id == entity.ToStatusId)
                .ToList();

            List<TransactionStatusTranslation> statusTranslations = _transactionStatusTranslationRepository.Query()
                .ToList()
                .Where(translation => translation.TransactionStatusId == entity.FromStatusId || translation.TransactionStatusId == entity.ToStatusId)
                .ToList();

            TransactionStatus? fromStatus = statuses.FirstOrDefault(status => status.Id == entity.FromStatusId);
            TransactionStatus? toStatus = statuses.FirstOrDefault(status => status.Id == entity.ToStatusId);

            return new GetByIdTransactionStatusTransitionResponse
            {
                Id = entity.Id,
                FromStatusId = entity.FromStatusId,
                FromStatusCode = fromStatus?.Code ?? string.Empty,
                FromStatusName = ResolveStatusName(statusTranslations, entity.FromStatusId, requestedLanguage.Id, defaultLanguage.Id),
                ToStatusId = entity.ToStatusId,
                ToStatusCode = toStatus?.Code ?? string.Empty,
                ToStatusName = ResolveStatusName(statusTranslations, entity.ToStatusId, requestedLanguage.Id, defaultLanguage.Id),
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

        private static string ResolveStatusName(
            IEnumerable<TransactionStatusTranslation> translations,
            int statusId,
            Guid requestedLanguageId,
            Guid defaultLanguageId)
        {
            TransactionStatusTranslation? translation = translations
                .Where(item => item.TransactionStatusId == statusId)
                .FirstOrDefault(item => item.LanguageId == requestedLanguageId)
                ?? translations
                    .Where(item => item.TransactionStatusId == statusId)
                    .FirstOrDefault(item => item.LanguageId == defaultLanguageId);

            if (translation is null)
                return string.Empty;

            return (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(translation, "Name") ?? string.Empty;
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
