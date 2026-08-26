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
namespace Symplify.BackOffice.Application.Features.Countries.Commands.Update;
public class UpdateCountryCommand : IRequest<UpdatedCountryResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string? PhoneCode { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCountries";
    public string[] Roles => new[] { CountriesOperationClaims.Admin, CountriesOperationClaims.Write, CountriesOperationClaims.Update };
    public class UpdateCountryCommandHandler : IRequestHandler<UpdateCountryCommand, UpdatedCountryResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name" };
        private readonly ICountryRepository _repository; private readonly ICountryTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly IMapper _mapper; private readonly CountryBusinessRules _rules;
        public UpdateCountryCommandHandler(ICountryRepository repository, ICountryTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, IMapper mapper, CountryBusinessRules rules) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedCountryResponse> Handle(UpdateCountryCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            Country? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CountryShouldExistWhenSelected(entity);
            entity!.Code = request.Code;
            entity!.PhoneCode = request.PhoneCode;
            entity!.IsActive = request.IsActive;
            entity!.Order = request.Order;
            Country updatedEntity = await _repository.UpdateAsync(entity!);
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(x => x.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            List<CountryTranslation> existingTranslations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.CountryId, request.Id)).ToList();
            foreach (TranslationInputDto input in request.Translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId)) continue;
                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);
                if (!isDefaultLanguage && !hasAnyValue) continue;
                CountryTranslation? existingTranslation = existingTranslations.FirstOrDefault(x => x.LanguageId == input.LanguageId);
                if (existingTranslation is null)
                {
                    CountryTranslation translation = new();
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "CountryId", request.Id);
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                    LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);
                    await _translationRepository.AddAsync(translation);
                    continue;
                }
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(existingTranslation, TranslationFieldNames, input.Fields);
                await _translationRepository.UpdateAsync(existingTranslation);
            }
            return _mapper.Map<UpdatedCountryResponse>(updatedEntity);
        }
    }
}
