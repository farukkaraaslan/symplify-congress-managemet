using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.PaymentStatuses.Constants;
using Symplify.BackOffice.Application.Features.PaymentStatuses.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.PaymentStatuses.Commands.DeleteTranslation;
public class DeletePaymentStatusTranslationCommand : IRequest<DeletedPaymentStatusTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public int PaymentStatusId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetPaymentStatuses";
    public string[] Roles => new[] { PaymentStatusesOperationClaims.Admin, PaymentStatusesOperationClaims.Write, PaymentStatusesOperationClaims.Delete };
    public class DeletePaymentStatusTranslationCommandHandler : IRequestHandler<DeletePaymentStatusTranslationCommand, DeletedPaymentStatusTranslationResponse>
    {
        private readonly IPaymentStatusTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeletePaymentStatusTranslationCommandHandler(IPaymentStatusTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedPaymentStatusTranslationResponse> Handle(DeletePaymentStatusTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(PaymentStatusesMessages.DefaultTranslationCannotBeDeleted);
            PaymentStatusTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.PaymentStatusId.Equals(request.PaymentStatusId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(PaymentStatusesMessages.TranslationNotFound);
            PaymentStatusTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedPaymentStatusTranslationResponse { Id = deletedTranslation.Id, PaymentStatusId = deletedTranslation.PaymentStatusId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
