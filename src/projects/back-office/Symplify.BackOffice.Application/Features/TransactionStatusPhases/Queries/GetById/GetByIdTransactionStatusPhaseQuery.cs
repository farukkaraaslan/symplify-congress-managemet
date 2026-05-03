using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatusPhases.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Queries.GetById;

public class GetByIdTransactionStatusPhaseQuery : IRequest<GetByIdTransactionStatusPhaseResponse>, ISecuredRequest
{
    public int Id { get; set; }

    public Guid? LanguageId { get; set; }

    public string? Culture { get; set; }

    public string[] Roles => new[] { TransactionStatusPhasesOperationClaims.Admin, TransactionStatusPhasesOperationClaims.Read };

    public class GetByIdTransactionStatusPhaseQueryHandler
        : IRequestHandler<GetByIdTransactionStatusPhaseQuery, GetByIdTransactionStatusPhaseResponse>
    {
        private readonly ITransactionStatusPhaseRepository _repository;
        private readonly ITransactionStatusPhaseTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public GetByIdTransactionStatusPhaseQueryHandler(
            ITransactionStatusPhaseRepository repository,
            ITransactionStatusPhaseTranslationRepository translationRepository,
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

        public async Task<GetByIdTransactionStatusPhaseResponse> Handle(
            GetByIdTransactionStatusPhaseQuery request,
            CancellationToken cancellationToken)
        {
            TransactionStatusPhase? entity = await _repository.GetAsync(predicate: root => root.Id.Equals(request.Id));

            if (entity is null)
                throw new BusinessException(TransactionStatusPhasesMessages.EntityNotFound);

            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(
                request.LanguageId,
                request.Culture,
                defaultLanguage,
                cancellationToken);

            List<TransactionStatusPhaseTranslation> translations = _translationRepository.Query()
                .ToList()
                .Where(translation => translation.TransactionStatusPhaseId == request.Id)
                .ToList();

            TransactionStatusPhaseTranslation? requestedTranslation = translations
                .FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);

            TransactionStatusPhaseTranslation? displayTranslation = _fallbackResolver.Resolve(
                translations,
                requestedLanguage.Id,
                defaultLanguage.Id);

            return new GetByIdTransactionStatusPhaseResponse
            {
                Id = entity.Id,
                Code = entity.Code,
                Order = entity.Order,
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
