using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Constants;
using Symplify.BackOffice.Application.Features.CongressPaymentPlans.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressPaymentPlans.Commands.DeleteTranslation;
public class DeleteCongressPaymentPlanTranslationCommand : IRequest<DeletedCongressPaymentPlanTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressPaymentPlanId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressPaymentPlans";
    public string[] Roles => new[] { CongressPaymentPlansOperationClaims.Admin, CongressPaymentPlansOperationClaims.Write, CongressPaymentPlansOperationClaims.Delete };
    public class DeleteCongressPaymentPlanTranslationCommandHandler : IRequestHandler<DeleteCongressPaymentPlanTranslationCommand, DeletedCongressPaymentPlanTranslationResponse>
    {
        private readonly ICongressPaymentPlanTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteCongressPaymentPlanTranslationCommandHandler(ICongressPaymentPlanTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedCongressPaymentPlanTranslationResponse> Handle(DeleteCongressPaymentPlanTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(CongressPaymentPlansMessages.DefaultTranslationCannotBeDeleted);
            CongressPaymentPlanTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.CongressPaymentPlanId.Equals(request.CongressPaymentPlanId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(CongressPaymentPlansMessages.TranslationNotFound);
            CongressPaymentPlanTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedCongressPaymentPlanTranslationResponse { Id = deletedTranslation.Id, CongressPaymentPlanId = deletedTranslation.CongressPaymentPlanId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
