using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.SubmissionTypes.Constants;
using Symplify.BackOffice.Application.Features.SubmissionTypes.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.SubmissionTypes.Queries.GetForUpdate;
public class GetSubmissionTypeForUpdateQuery : IRequest<GetSubmissionTypeForUpdateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { SubmissionTypesOperationClaims.Admin, SubmissionTypesOperationClaims.Read };
    public class GetSubmissionTypeForUpdateQueryHandler : IRequestHandler<GetSubmissionTypeForUpdateQuery, GetSubmissionTypeForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };
        private readonly ISubmissionTypeRepository _repository; private readonly ISubmissionTypeTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public GetSubmissionTypeForUpdateQueryHandler(ISubmissionTypeRepository repository, ISubmissionTypeTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<GetSubmissionTypeForUpdateResponse> Handle(GetSubmissionTypeForUpdateQuery request, CancellationToken cancellationToken)
        {
            SubmissionType? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            if (entity is null) throw new BusinessException(SubmissionTypesMessages.EntityNotFound);
            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            List<SubmissionTypeTranslation> translations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.SubmissionTypeId, request.Id)).ToList();
            return new GetSubmissionTypeForUpdateResponse { Id = entity.Id,
                Code = entity.Code,
                Order = entity.Order,
                IsActive = entity.IsActive,
                Translations = languages.Select(language => { SubmissionTypeTranslation? translation = translations.FirstOrDefault(x => x.LanguageId == language.Id); return new LocalizedTranslationDto { LanguageId = language.Id, Culture = language.Culture, LanguageName = language.Name, IsDefault = language.IsDefault, Exists = translation is not null, Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames) }; }).ToList() };
        }
    }
}
