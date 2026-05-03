using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.TransactionStatusPhases.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Commands.DeleteTranslation;

public class DeleteTransactionStatusPhaseTranslationCommand
    : IRequest<DeletedTransactionStatusPhaseTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public int TransactionStatusPhaseId { get; set; }

    public Guid LanguageId { get; set; }

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetTransactionStatusPhases";

    public string[] Roles => new[]
    {
        TransactionStatusPhasesOperationClaims.Admin,
        TransactionStatusPhasesOperationClaims.Write,
        TransactionStatusPhasesOperationClaims.Delete
    };

    public class DeleteTransactionStatusPhaseTranslationCommandHandler
        : IRequestHandler<DeleteTransactionStatusPhaseTranslationCommand, DeletedTransactionStatusPhaseTranslationResponse>
    {
        private readonly ITransactionStatusPhaseTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;

        public DeleteTransactionStatusPhaseTranslationCommandHandler(
            ITransactionStatusPhaseTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider)
        {
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
        }

        public async Task<DeletedTransactionStatusPhaseTranslationResponse> Handle(
            DeleteTransactionStatusPhaseTranslationCommand request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

            if (request.LanguageId == defaultLanguage.Id)
                throw new BusinessException(TransactionStatusPhasesMessages.DefaultTranslationCannotBeDeleted);

            TransactionStatusPhaseTranslation? translation = _translationRepository.Query()
                .FirstOrDefault(x =>
                    x.TransactionStatusPhaseId == request.TransactionStatusPhaseId &&
                    x.LanguageId == request.LanguageId);

            if (translation is null)
                throw new BusinessException(TransactionStatusPhasesMessages.TranslationNotFound);

            TransactionStatusPhaseTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);

            return new DeletedTransactionStatusPhaseTranslationResponse
            {
                Id = deletedTranslation.Id,
                TransactionStatusPhaseId = deletedTranslation.TransactionStatusPhaseId,
                LanguageId = deletedTranslation.LanguageId
            };
        }
    }
}
