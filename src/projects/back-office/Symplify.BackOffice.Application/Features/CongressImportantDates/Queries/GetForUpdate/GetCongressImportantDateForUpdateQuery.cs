using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Constants;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Queries.GetForUpdate;
public class GetCongressImportantDateForUpdateQuery : IRequest<GetCongressImportantDateForUpdateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { CongressImportantDatesOperationClaims.Admin, CongressImportantDatesOperationClaims.Read };
    public class GetCongressImportantDateForUpdateQueryHandler : IRequestHandler<GetCongressImportantDateForUpdateQuery, GetCongressImportantDateForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Title", "Description" };
        private readonly ICongressImportantDateRepository _repository; private readonly ICongressImportantDateTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public GetCongressImportantDateForUpdateQueryHandler(ICongressImportantDateRepository repository, ICongressImportantDateTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<GetCongressImportantDateForUpdateResponse> Handle(GetCongressImportantDateForUpdateQuery request, CancellationToken cancellationToken)
        {
            CongressImportantDate? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            if (entity is null) throw new BusinessException(CongressImportantDatesMessages.EntityNotFound);
            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            List<CongressImportantDateTranslation> translations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.CongressImportantDateId, request.Id)).ToList();
            return new GetCongressImportantDateForUpdateResponse { Id = entity.Id,
                CongressId = entity.CongressId,
                Date = entity.Date,
                Order = entity.Order,
                IsActive = entity.IsActive,
                Translations = languages.Select(language => { CongressImportantDateTranslation? translation = translations.FirstOrDefault(x => x.LanguageId == language.Id); return new LocalizedTranslationDto { LanguageId = language.Id, Culture = language.Culture, LanguageName = language.Name, IsDefault = language.IsDefault, Exists = translation is not null, Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames) }; }).ToList() };
        }
    }
}
