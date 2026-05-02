using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressSliders.Constants;
using Symplify.BackOffice.Application.Features.CongressSliders.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressSliders.Queries.GetForUpdate;
public class GetCongressSliderForUpdateQuery : IRequest<GetCongressSliderForUpdateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { CongressSlidersOperationClaims.Admin, CongressSlidersOperationClaims.Read };
    public class GetCongressSliderForUpdateQueryHandler : IRequestHandler<GetCongressSliderForUpdateQuery, GetCongressSliderForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Title", "Subtitle", "ButtonText", "ButtonUrl" };
        private readonly ICongressSliderRepository _repository; private readonly ICongressSliderTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public GetCongressSliderForUpdateQueryHandler(ICongressSliderRepository repository, ICongressSliderTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<GetCongressSliderForUpdateResponse> Handle(GetCongressSliderForUpdateQuery request, CancellationToken cancellationToken)
        {
            CongressSlider? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            if (entity is null) throw new BusinessException(CongressSlidersMessages.EntityNotFound);
            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            List<CongressSliderTranslation> translations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.CongressSliderId, request.Id)).ToList();
            return new GetCongressSliderForUpdateResponse { Id = entity.Id,
                CongressId = entity.CongressId,
                ImagePath = entity.ImagePath,
                Order = entity.Order,
                IsActive = entity.IsActive,
                Translations = languages.Select(language => { CongressSliderTranslation? translation = translations.FirstOrDefault(x => x.LanguageId == language.Id); return new LocalizedTranslationDto { LanguageId = language.Id, Culture = language.Culture, LanguageName = language.Name, IsDefault = language.IsDefault, Exists = translation is not null, Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames) }; }).ToList() };
        }
    }
}
