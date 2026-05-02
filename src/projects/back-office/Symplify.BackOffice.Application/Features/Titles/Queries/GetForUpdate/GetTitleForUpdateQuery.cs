using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.Titles.Constants;
using Symplify.BackOffice.Application.Features.Titles.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Domain.Lookups;
namespace Symplify.BackOffice.Application.Features.Titles.Queries.GetForUpdate;
public class GetTitleForUpdateQuery : IRequest<GetTitleForUpdateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { TitlesOperationClaims.Admin, TitlesOperationClaims.Read };
    public class GetTitleForUpdateQueryHandler : IRequestHandler<GetTitleForUpdateQuery, GetTitleForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };
        private readonly ITitleRepository _repository; private readonly ITitleTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public GetTitleForUpdateQueryHandler(ITitleRepository repository, ITitleTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<GetTitleForUpdateResponse> Handle(GetTitleForUpdateQuery request, CancellationToken cancellationToken)
        {
            Title? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            if (entity is null) throw new BusinessException(TitlesMessages.EntityNotFound);
            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            List<TitleTranslation> translations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.TitleId, request.Id)).ToList();
            return new GetTitleForUpdateResponse { Id = entity.Id,
                Code = entity.Code,
                Order = entity.Order,
                IsActive = entity.IsActive,
                Translations = languages.Select(language => { TitleTranslation? translation = translations.FirstOrDefault(x => x.LanguageId == language.Id); return new LocalizedTranslationDto { LanguageId = language.Id, Culture = language.Culture, LanguageName = language.Name, IsDefault = language.IsDefault, Exists = translation is not null, Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames) }; }).ToList() };
        }
    }
}
