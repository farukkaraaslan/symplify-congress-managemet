using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressBoards.Constants;
using Symplify.BackOffice.Application.Features.CongressBoards.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressBoards.Commands.Update;

public class UpdateCongressBoardCommand : IRequest<UpdatedCongressBoardResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressBoards";
    public string[] Roles => new[] { CongressBoardsOperationClaims.Admin, CongressBoardsOperationClaims.Write, CongressBoardsOperationClaims.Update };

    public class UpdateCongressBoardCommandHandler : IRequestHandler<UpdateCongressBoardCommand, UpdatedCongressBoardResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };

        private readonly ICongressBoardRepository _repository;
        private readonly ICongressBoardTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly CongressBoardBusinessRules _rules;

        public UpdateCongressBoardCommandHandler(
            ICongressBoardRepository repository,
            ICongressBoardTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            IMapper mapper,
            CongressBoardBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<UpdatedCongressBoardResponse> Handle(
            UpdateCongressBoardCommand request,
            CancellationToken cancellationToken)
        {
            await _rules.CongressShouldBeSelected(request.CongressId);
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);

            CongressBoard? entity = await _repository.GetAsync(predicate: board => board.Id.Equals(request.Id));
            await _rules.CongressBoardShouldExistWhenSelected(entity);

            entity!.CongressId = request.CongressId;
            entity.Order = request.Order > 0 ? request.Order : entity.Order;
            entity.IsActive = request.IsActive;

            CongressBoard updatedEntity = await _repository.UpdateAsync(entity);
            await UpsertTranslationsAsync(updatedEntity.Id, request.Translations, cancellationToken);

            return _mapper.Map<UpdatedCongressBoardResponse>(updatedEntity);
        }

        private async Task UpsertTranslationsAsync(
            Guid congressBoardId,
            IEnumerable<TranslationInputDto> translations,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(language => language.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

            List<CongressBoardTranslation> existingTranslations = _translationRepository
                .Query()
                .ToList()
                .Where(translation => translation.CongressBoardId == congressBoardId && !IsDeleted(translation))
                .ToList();

            foreach (TranslationInputDto input in translations.GroupBy(translation => translation.LanguageId).Select(group => group.First()))
            {
                if (!activeLanguageIds.Contains(input.LanguageId))
                    continue;

                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);

                if (!isDefaultLanguage && !hasAnyValue)
                    continue;

                CongressBoardTranslation? existingTranslation = existingTranslations.FirstOrDefault(translation => translation.LanguageId == input.LanguageId);

                if (existingTranslation is null)
                {
                    CongressBoardTranslation translation = new();
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "CongressBoardId", congressBoardId);
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                    LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);

                    await _translationRepository.AddAsync(translation);
                    continue;
                }

                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(existingTranslation, TranslationFieldNames, input.Fields);
                await _translationRepository.UpdateAsync(existingTranslation);
            }
        }

        private static bool IsDeleted(object entity)
            => LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate") is not null;
    }
}
