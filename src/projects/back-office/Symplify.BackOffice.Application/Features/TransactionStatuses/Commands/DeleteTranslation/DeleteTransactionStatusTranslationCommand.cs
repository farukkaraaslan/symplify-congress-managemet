using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.DeleteTranslation;

public class DeleteTransactionStatusTranslationCommand : IRequest<DeletedTransactionStatusTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public int TransactionStatusId { get; set; }

    public Guid LanguageId { get; set; }

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetTransactionStatuses";

    public string[] Roles => new[]
    {
        TransactionStatusesOperationClaims.Admin,
        TransactionStatusesOperationClaims.Write,
        TransactionStatusesOperationClaims.Delete
    };

    public class DeleteTransactionStatusTranslationCommandHandler
        : IRequestHandler<DeleteTransactionStatusTranslationCommand, DeletedTransactionStatusTranslationResponse>
    {
        private readonly ITransactionStatusTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;

        public DeleteTransactionStatusTranslationCommandHandler(
            ITransactionStatusTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider)
        {
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
        }

        public async Task<DeletedTransactionStatusTranslationResponse> Handle(
            DeleteTransactionStatusTranslationCommand request,
            CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

            if (request.LanguageId == defaultLanguage.Id)
                throw new BusinessException(TransactionStatusesMessages.DefaultTranslationCannotBeDeleted);

            TransactionStatusTranslation? translation = _translationRepository.Query()
                .FirstOrDefault(entity =>
                    entity.TransactionStatusId == request.TransactionStatusId &&
                    entity.LanguageId == request.LanguageId);

            if (translation is null)
                throw new BusinessException(TransactionStatusesMessages.TranslationNotFound);

            TransactionStatusTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);

            return new DeletedTransactionStatusTranslationResponse
            {
                Id = deletedTranslation.Id,
                TransactionStatusId = deletedTranslation.TransactionStatusId,
                LanguageId = deletedTranslation.LanguageId
            };
        }
    }
}
