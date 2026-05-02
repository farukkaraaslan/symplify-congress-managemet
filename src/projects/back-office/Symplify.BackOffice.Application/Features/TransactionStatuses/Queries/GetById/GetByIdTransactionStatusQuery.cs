using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Constants;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Queries.GetById;
public class GetByIdTransactionStatusQuery : IRequest<GetByIdTransactionStatusResponse>, ISecuredRequest
{
    public int Id { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }
    public string[] Roles => new[] { TransactionStatusesOperationClaims.Admin, TransactionStatusesOperationClaims.Read };
    public class GetByIdTransactionStatusQueryHandler : IRequestHandler<GetByIdTransactionStatusQuery, GetByIdTransactionStatusResponse>
    {
        private readonly ITransactionStatusRepository _repository; private readonly ITransactionStatusTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly ICurrentLanguageProvider _currentLanguageProvider; private readonly ITranslationFallbackResolver _fallbackResolver;
        public GetByIdTransactionStatusQueryHandler(ITransactionStatusRepository repository, ITransactionStatusTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, ICurrentLanguageProvider currentLanguageProvider, ITranslationFallbackResolver fallbackResolver) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _currentLanguageProvider = currentLanguageProvider; _fallbackResolver = fallbackResolver; }
        public async Task<GetByIdTransactionStatusResponse> Handle(GetByIdTransactionStatusQuery request, CancellationToken cancellationToken)
        {
            TransactionStatus? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            if (entity is null) throw new BusinessException(TransactionStatusesMessages.EntityNotFound);
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = request.LanguageId.HasValue ? await _languageProvider.GetByIdAsync(request.LanguageId.Value, cancellationToken) ?? defaultLanguage : !string.IsNullOrWhiteSpace(request.Culture) ? await _languageProvider.GetByCultureAsync(request.Culture, cancellationToken) ?? defaultLanguage : await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
            List<TransactionStatusTranslation> translations = _translationRepository.Query().ToList().Where(x => EqualityComparer<int>.Default.Equals(x.TransactionStatusId, request.Id)).ToList();
            TransactionStatusTranslation? requestedTranslation = translations.FirstOrDefault(x => x.LanguageId == requestedLanguage.Id);
            TransactionStatusTranslation? displayTranslation = _fallbackResolver.Resolve(translations, requestedLanguage.Id, defaultLanguage.Id);
            return new GetByIdTransactionStatusResponse { Id = entity.Id,
                Code = entity.Code,
                Order = entity.Order,
                IsActive = entity.IsActive,
                Name = displayTranslation is null ? string.Empty : (string)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Name")!,
                Description = displayTranslation is null ? null : (string?)LocalizedEntityRuntimeHelper.GetPropertyValue(displayTranslation, "Description")!,
                DisplayLanguageId = displayTranslation?.LanguageId ?? default, IsFallback = requestedTranslation is null && displayTranslation is not null };
        }
    }
}
