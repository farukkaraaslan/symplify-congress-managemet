using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Cities.Constants;
using Symplify.BackOffice.Application.Features.Cities.Rules;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Domain.Reference.Translations;
namespace Symplify.BackOffice.Application.Features.Cities.Commands.Create;
public class CreateCityCommand : IRequest<CreatedCityResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid StateId { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCities";
    public string[] Roles => new[] { CitiesOperationClaims.Admin, CitiesOperationClaims.Write, CitiesOperationClaims.Add };
    public class CreateCityCommandHandler : IRequestHandler<CreateCityCommand, CreatedCityResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name" };
        private readonly ICityRepository _repository; private readonly ICityTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly IMapper _mapper; private readonly CityBusinessRules _rules;
        public CreateCityCommandHandler(ICityRepository repository, ICityTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, IMapper mapper, CityBusinessRules rules) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _mapper = mapper; _rules = rules; }
        public async Task<CreatedCityResponse> Handle(CreateCityCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            City entity = new()
            {
                Id = Guid.NewGuid(),
                StateId = request.StateId,
                IsActive = request.IsActive,
                Order = request.Order,
            };
            City createdEntity = await _repository.AddAsync(entity);
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(x => x.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            foreach (TranslationInputDto input in request.Translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId)) continue;
                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);
                if (!isDefaultLanguage && !hasAnyValue) continue;
                CityTranslation translation = new();
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "CityId", createdEntity.Id);
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);
                await _translationRepository.AddAsync(translation);
            }
            return _mapper.Map<CreatedCityResponse>(createdEntity);
        }
    }
}
