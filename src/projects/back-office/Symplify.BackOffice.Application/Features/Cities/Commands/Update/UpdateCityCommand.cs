using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.Cities.Constants;
using Symplify.BackOffice.Application.Features.Cities.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Domain.Reference.Translations;
namespace Symplify.BackOffice.Application.Features.Cities.Commands.Update;
public class UpdateCityCommand : IRequest<UpdatedCityResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid StateId { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCities";
    public string[] Roles => new[] { CitiesOperationClaims.Admin, CitiesOperationClaims.Write, CitiesOperationClaims.Update };
    public class UpdateCityCommandHandler : IRequestHandler<UpdateCityCommand, UpdatedCityResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name" };
        private readonly ICityRepository _repository; private readonly ICityTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly IMapper _mapper; private readonly CityBusinessRules _rules;
        public UpdateCityCommandHandler(ICityRepository repository, ICityTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, IMapper mapper, CityBusinessRules rules) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedCityResponse> Handle(UpdateCityCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            City? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CityShouldExistWhenSelected(entity);
            entity!.StateId = request.StateId;
            entity!.IsActive = request.IsActive;
            entity!.Order = request.Order;
            City updatedEntity = await _repository.UpdateAsync(entity!);
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(x => x.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            List<CityTranslation> existingTranslations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.CityId, request.Id)).ToList();
            foreach (TranslationInputDto input in request.Translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId)) continue;
                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);
                if (!isDefaultLanguage && !hasAnyValue) continue;
                CityTranslation? existingTranslation = existingTranslations.FirstOrDefault(x => x.LanguageId == input.LanguageId);
                if (existingTranslation is null)
                {
                    CityTranslation translation = new();
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "CityId", request.Id);
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                    LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);
                    await _translationRepository.AddAsync(translation);
                    continue;
                }
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(existingTranslation, TranslationFieldNames, input.Fields);
                await _translationRepository.UpdateAsync(existingTranslation);
            }
            return _mapper.Map<UpdatedCityResponse>(updatedEntity);
        }
    }
}
