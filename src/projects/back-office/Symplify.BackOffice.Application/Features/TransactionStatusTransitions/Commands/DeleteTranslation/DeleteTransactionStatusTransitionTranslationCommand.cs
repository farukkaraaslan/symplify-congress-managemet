using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Constants;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.DeleteTranslation;
public class DeleteTransactionStatusTransitionTranslationCommand : IRequest<DeletedTransactionStatusTransitionTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public int TransactionStatusTransitionId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetTransactionStatusTransitions";
    public string[] Roles => new[] { TransactionStatusTransitionsOperationClaims.Admin, TransactionStatusTransitionsOperationClaims.Write, TransactionStatusTransitionsOperationClaims.Delete };
    public class DeleteTransactionStatusTransitionTranslationCommandHandler : IRequestHandler<DeleteTransactionStatusTransitionTranslationCommand, DeletedTransactionStatusTransitionTranslationResponse>
    {
        private readonly ITransactionStatusTransitionTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteTransactionStatusTransitionTranslationCommandHandler(ITransactionStatusTransitionTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedTransactionStatusTransitionTranslationResponse> Handle(DeleteTransactionStatusTransitionTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(TransactionStatusTransitionsMessages.DefaultTranslationCannotBeDeleted);
            TransactionStatusTransitionTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.TransactionStatusTransitionId.Equals(request.TransactionStatusTransitionId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(TransactionStatusTransitionsMessages.TranslationNotFound);
            TransactionStatusTransitionTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedTransactionStatusTransitionTranslationResponse { Id = deletedTranslation.Id, TransactionStatusTransitionId = deletedTranslation.TransactionStatusTransitionId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
