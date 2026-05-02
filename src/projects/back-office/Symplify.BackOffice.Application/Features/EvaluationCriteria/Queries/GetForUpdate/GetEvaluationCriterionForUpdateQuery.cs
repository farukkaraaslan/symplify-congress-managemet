using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.EvaluationCriteria.Constants;
using Symplify.BackOffice.Application.Features.EvaluationCriteria.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.EvaluationCriteria.Queries.GetForUpdate;
public class GetEvaluationCriterionForUpdateQuery : IRequest<GetEvaluationCriterionForUpdateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { EvaluationCriteriaOperationClaims.Admin, EvaluationCriteriaOperationClaims.Read };
    public class GetEvaluationCriterionForUpdateQueryHandler : IRequestHandler<GetEvaluationCriterionForUpdateQuery, GetEvaluationCriterionForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };
        private readonly IEvaluationCriterionRepository _repository; private readonly IEvaluationCriterionTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public GetEvaluationCriterionForUpdateQueryHandler(IEvaluationCriterionRepository repository, IEvaluationCriterionTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<GetEvaluationCriterionForUpdateResponse> Handle(GetEvaluationCriterionForUpdateQuery request, CancellationToken cancellationToken)
        {
            EvaluationCriterion? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            if (entity is null) throw new BusinessException(EvaluationCriteriaMessages.EntityNotFound);
            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            List<EvaluationCriterionTranslation> translations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.EvaluationCriterionId, request.Id)).ToList();
            return new GetEvaluationCriterionForUpdateResponse { Id = entity.Id,
                Code = entity.Code,
                Order = entity.Order,
                IsActive = entity.IsActive,
                Translations = languages.Select(language => { EvaluationCriterionTranslation? translation = translations.FirstOrDefault(x => x.LanguageId == language.Id); return new LocalizedTranslationDto { LanguageId = language.Id, Culture = language.Culture, LanguageName = language.Name, IsDefault = language.IsDefault, Exists = translation is not null, Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames) }; }).ToList() };
        }
    }
}
