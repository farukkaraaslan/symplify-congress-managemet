using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Countries.Constants;
using Symplify.BackOffice.Application.Features.Countries.Rules;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Domain.Reference.Translations;
namespace Symplify.BackOffice.Application.Features.Countries.Commands.Create;
public class CreateCountryCommand : IRequest<CreatedCountryResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public string? Code { get; set; }
    public string? PhoneCode { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCountries";
    public string[] Roles => new[] { CountriesOperationClaims.Admin, CountriesOperationClaims.Write, CountriesOperationClaims.Add };
    public class CreateCountryCommandHandler : IRequestHandler<CreateCountryCommand, CreatedCountryResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name" };
        private readonly ICountryRepository _repository; private readonly ICountryTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly IMapper _mapper; private readonly CountryBusinessRules _rules;
        public CreateCountryCommandHandler(ICountryRepository repository, ICountryTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, IMapper mapper, CountryBusinessRules rules) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _mapper = mapper; _rules = rules; }
        public async Task<CreatedCountryResponse> Handle(CreateCountryCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            Country entity = new()
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                PhoneCode = request.PhoneCode,
                IsActive = request.IsActive,
                Order = request.Order,
            };
            Country createdEntity = await _repository.AddAsync(entity);
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(x => x.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            foreach (TranslationInputDto input in request.Translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId)) continue;
                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);
                if (!isDefaultLanguage && !hasAnyValue) continue;
                CountryTranslation translation = new();
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "CountryId", createdEntity.Id);
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);
                await _translationRepository.AddAsync(translation);
            }
            return _mapper.Map<CreatedCountryResponse>(createdEntity);
        }
    }
}
