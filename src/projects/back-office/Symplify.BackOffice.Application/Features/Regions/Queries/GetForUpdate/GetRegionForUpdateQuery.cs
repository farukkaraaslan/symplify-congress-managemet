using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.Regions.Constants;
using Symplify.BackOffice.Application.Features.Regions.Constants;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Domain.Reference.Translations;
namespace Symplify.BackOffice.Application.Features.Regions.Queries.GetForUpdate;
public class GetRegionForUpdateQuery : IRequest<GetRegionForUpdateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { RegionsOperationClaims.Admin, RegionsOperationClaims.Read };
    public class GetRegionForUpdateQueryHandler : IRequestHandler<GetRegionForUpdateQuery, GetRegionForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name" };
        private readonly IRegionRepository _repository; private readonly IRegionTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider;
        public GetRegionForUpdateQueryHandler(IRegionRepository repository, IRegionTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; }
        public async Task<GetRegionForUpdateResponse> Handle(GetRegionForUpdateQuery request, CancellationToken cancellationToken)
        {
            Region? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            if (entity is null) throw new BusinessException(RegionsMessages.EntityNotFound);
            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            List<RegionTranslation> translations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.RegionId, request.Id)).ToList();
            return new GetRegionForUpdateResponse { Id = entity.Id,
                CountryId = entity.CountryId,
                IsActive = entity.IsActive,
                Order = entity.Order,
                Translations = languages.Select(language => { RegionTranslation? translation = translations.FirstOrDefault(x => x.LanguageId == language.Id); return new LocalizedTranslationDto { LanguageId = language.Id, Culture = language.Culture, LanguageName = language.Name, IsDefault = language.IsDefault, Exists = translation is not null, Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames) }; }).ToList() };
        }
    }
}
