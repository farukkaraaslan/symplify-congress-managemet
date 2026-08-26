using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Regions.Constants;
using Symplify.BackOffice.Application.Features.Regions.Rules;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Domain.Reference.Translations;
namespace Symplify.BackOffice.Application.Features.Regions.Commands.Update;
public class UpdateRegionCommand : IRequest<UpdatedRegionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid? CountryId { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetRegions";
    public string[] Roles => new[] { RegionsOperationClaims.Admin, RegionsOperationClaims.Write, RegionsOperationClaims.Update };
    public class UpdateRegionCommandHandler : IRequestHandler<UpdateRegionCommand, UpdatedRegionResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name" };
        private readonly IRegionRepository _repository; private readonly IRegionTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly IMapper _mapper; private readonly RegionBusinessRules _rules;
        public UpdateRegionCommandHandler(IRegionRepository repository, IRegionTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, IMapper mapper, RegionBusinessRules rules) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedRegionResponse> Handle(UpdateRegionCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            Region? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.RegionShouldExistWhenSelected(entity);
            entity!.CountryId = request.CountryId;
            entity!.IsActive = request.IsActive;
            entity!.Order = request.Order;
            Region updatedEntity = await _repository.UpdateAsync(entity!);
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(x => x.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            List<RegionTranslation> existingTranslations = _translationRepository.Query().ToList().Where(x => EqualityComparer<Guid>.Default.Equals(x.RegionId, request.Id)).ToList();
            foreach (TranslationInputDto input in request.Translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId)) continue;
                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);
                if (!isDefaultLanguage && !hasAnyValue) continue;
                RegionTranslation? existingTranslation = existingTranslations.FirstOrDefault(x => x.LanguageId == input.LanguageId);
                if (existingTranslation is null)
                {
                    RegionTranslation translation = new();
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "RegionId", request.Id);
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                    LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);
                    await _translationRepository.AddAsync(translation);
                    continue;
                }
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(existingTranslation, TranslationFieldNames, input.Fields);
                await _translationRepository.UpdateAsync(existingTranslation);
            }
            return _mapper.Map<UpdatedRegionResponse>(updatedEntity);
        }
    }
}
