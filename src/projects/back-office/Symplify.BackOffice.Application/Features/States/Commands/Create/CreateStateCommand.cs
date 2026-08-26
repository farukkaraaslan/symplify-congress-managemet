using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.States.Constants;
using Symplify.BackOffice.Application.Features.States.Rules;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
using Symplify.BackOffice.Domain.Reference.Translations;
namespace Symplify.BackOffice.Application.Features.States.Commands.Create;
public class CreateStateCommand : IRequest<CreatedStateResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CountryId { get; set; }
    public string? Code { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetStates";
    public string[] Roles => new[] { StatesOperationClaims.Admin, StatesOperationClaims.Write, StatesOperationClaims.Add };
    public class CreateStateCommandHandler : IRequestHandler<CreateStateCommand, CreatedStateResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name" };
        private readonly IStateRepository _repository; private readonly IStateTranslationRepository _translationRepository; private readonly IApplicationLanguageProvider _languageProvider; private readonly IMapper _mapper; private readonly StateBusinessRules _rules;
        public CreateStateCommandHandler(IStateRepository repository, IStateTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, IMapper mapper, StateBusinessRules rules) { _repository = repository; _translationRepository = translationRepository; _languageProvider = languageProvider; _mapper = mapper; _rules = rules; }
        public async Task<CreatedStateResponse> Handle(CreateStateCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            State entity = new()
            {
                Id = Guid.NewGuid(),
                CountryId = request.CountryId,
                Code = request.Code,
                IsActive = request.IsActive,
                Order = request.Order,
            };
            State createdEntity = await _repository.AddAsync(entity);
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(x => x.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            foreach (TranslationInputDto input in request.Translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId)) continue;
                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);
                if (!isDefaultLanguage && !hasAnyValue) continue;
                StateTranslation translation = new();
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "StateId", createdEntity.Id);
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);
                await _translationRepository.AddAsync(translation);
            }
            return _mapper.Map<CreatedStateResponse>(createdEntity);
        }
    }
}
