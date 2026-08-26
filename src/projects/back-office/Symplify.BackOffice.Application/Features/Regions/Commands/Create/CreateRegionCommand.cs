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
namespace Symplify.BackOffice.Application.Features.Regions.Commands.Create;
public class CreateRegionCommand : IRequest<CreatedRegionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid? CountryId { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetRegions";
    public string[] Roles => new[] { RegionsOperationClaims.Admin, RegionsOperationClaims.Write, RegionsOperationClaims.Add };
    public class CreateRegionCommandHandler : IRequestHandler<CreateRegionCommand, CreatedRegionResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name" };
        private readonly IRegionRepository _repository; private readonly IRegionTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly IMapper _mapper; private readonly RegionBusinessRules _rules;
        public CreateRegionCommandHandler(IRegionRepository repository, IRegionTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, IMapper mapper, RegionBusinessRules rules) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _mapper = mapper; _rules = rules; }
        public async Task<CreatedRegionResponse> Handle(CreateRegionCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            Region entity = new()
            {
                Id = Guid.NewGuid(),
                CountryId = request.CountryId,
                IsActive = request.IsActive,
                Order = request.Order,
            };
            Region createdEntity = await _repository.AddAsync(entity);
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(x => x.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            foreach (TranslationInputDto input in request.Translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId)) continue;
                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);
                if (!isDefaultLanguage && !hasAnyValue) continue;
                RegionTranslation translation = new();
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "RegionId", createdEntity.Id);
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);
                await _translationRepository.AddAsync(translation);
            }
            return _mapper.Map<CreatedRegionResponse>(createdEntity);
        }
    }
}
