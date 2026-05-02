using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.EvaluationCriteria.Constants;
using Symplify.BackOffice.Application.Features.EvaluationCriteria.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.EvaluationCriteria.Commands.DeleteTranslation;
public class DeleteEvaluationCriterionTranslationCommand : IRequest<DeletedEvaluationCriterionTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid EvaluationCriterionId { get; set; }
    public Guid LanguageId { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetEvaluationCriteria";
    public string[] Roles => new[] { EvaluationCriteriaOperationClaims.Admin, EvaluationCriteriaOperationClaims.Write, EvaluationCriteriaOperationClaims.Delete };
    public class DeleteEvaluationCriterionTranslationCommandHandler : IRequestHandler<DeleteEvaluationCriterionTranslationCommand, DeletedEvaluationCriterionTranslationResponse>
    {
        private readonly IEvaluationCriterionTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public DeleteEvaluationCriterionTranslationCommandHandler(IEvaluationCriterionTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<DeletedEvaluationCriterionTranslationResponse> Handle(DeleteEvaluationCriterionTranslationCommand request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            if (request.LanguageId == defaultLanguage.Id) throw new BusinessException(EvaluationCriteriaMessages.DefaultTranslationCannotBeDeleted);
            EvaluationCriterionTranslation? translation = _translationRepository.Query().FirstOrDefault(x => x.EvaluationCriterionId.Equals(request.EvaluationCriterionId) && x.LanguageId == request.LanguageId);
            if (translation is null) throw new BusinessException(EvaluationCriteriaMessages.TranslationNotFound);
            EvaluationCriterionTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation);
            return new DeletedEvaluationCriterionTranslationResponse { Id = deletedTranslation.Id, EvaluationCriterionId = deletedTranslation.EvaluationCriterionId, LanguageId = deletedTranslation.LanguageId };
        }
    }
}
