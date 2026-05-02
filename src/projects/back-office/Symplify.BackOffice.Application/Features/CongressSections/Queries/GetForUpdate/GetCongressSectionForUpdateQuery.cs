using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressSections.Constants;
using Symplify.BackOffice.Application.Features.CongressSections.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSections.Queries.GetForUpdate;
public class GetCongressSectionForUpdateQuery : IRequest<GetCongressSectionForUpdateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { CongressSectionsOperationClaims.Admin, CongressSectionsOperationClaims.Read };
    public class GetCongressSectionForUpdateQueryHandler : IRequestHandler<GetCongressSectionForUpdateQuery, GetCongressSectionForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Title", "Content" };
        private readonly ICongressSectionRepository _repository; private readonly ICongressSectionTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public GetCongressSectionForUpdateQueryHandler(ICongressSectionRepository repository, ICongressSectionTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<GetCongressSectionForUpdateResponse> Handle(GetCongressSectionForUpdateQuery request, CancellationToken cancellationToken)
        {
            CongressSection? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            if (entity is null) throw new BusinessException(CongressSectionsMessages.EntityNotFound);
            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            List<CongressSectionTranslation> translations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.CongressSectionId, request.Id)).ToList();
            return new GetCongressSectionForUpdateResponse { Id = entity.Id,
                CongressId = entity.CongressId,
                BindingKey = entity.BindingKey,
                Order = entity.Order,
                IsActive = entity.IsActive,
                Translations = languages.Select(language => { CongressSectionTranslation? translation = translations.FirstOrDefault(x => x.LanguageId == language.Id); return new LocalizedTranslationDto { LanguageId = language.Id, Culture = language.Culture, LanguageName = language.Name, IsDefault = language.IsDefault, Exists = translation is not null, Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames) }; }).ToList() };
        }
    }
}
