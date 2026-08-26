using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressSections.Constants;
using Symplify.BackOffice.Application.Features.CongressSections.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressSections.Commands.Update;

public class UpdateCongressSectionCommand
    : IRequest<UpdatedCongressSectionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public string BindingKey { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressSections";

    public string[] Roles => new[]
    {
        CongressSectionsOperationClaims.Admin,
        CongressSectionsOperationClaims.Write,
        CongressSectionsOperationClaims.Update
    };

    public class UpdateCongressSectionCommandHandler
        : IRequestHandler<UpdateCongressSectionCommand, UpdatedCongressSectionResponse>
    {
        private static readonly string[] TranslationFieldNames = { "Title", "Content" };

        private readonly ICongressSectionRepository _repository;
        private readonly ICongressSectionTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly CongressSectionBusinessRules _rules;

        public UpdateCongressSectionCommandHandler(
            ICongressSectionRepository repository,
            ICongressSectionTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            IMapper mapper,
            CongressSectionBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<UpdatedCongressSectionResponse> Handle(
            UpdateCongressSectionCommand request,
            CancellationToken cancellationToken)
        {
            request.BindingKey = (request.BindingKey ?? string.Empty).Trim();

            await _rules.CongressShouldExist(request.CongressId, cancellationToken);
            await _rules.BindingKeyShouldBeValid(request.BindingKey);
            await _rules.OrderShouldBeValid(request.Order);
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            await _rules.TranslationTitlesShouldBeValid(request.Translations, cancellationToken);

            CongressSection? entity = await _repository.GetAsync(predicate: item => item.Id == request.Id);
            await _rules.CongressSectionShouldExistWhenSelected(entity);
            await _rules.SectionShouldBelongToCongress(entity!, request.CongressId);
            await _rules.BindingKeyShouldBeUniqueForCongress(request.CongressId, request.BindingKey, request.Id);

            int requestedOrder = request.Order <= 0 ? entity!.Order : request.Order;

            entity!.CongressId = request.CongressId;
            entity.BindingKey = request.BindingKey;
            entity.Order = 0;
            entity.IsActive = request.IsActive;

            CongressSection updatedEntity = await _repository.UpdateAsync(entity);

            await NormalizeVisibleOrdersAsync(updatedEntity, requestedOrder, cancellationToken);
            await UpsertTranslationsAsync(updatedEntity.Id, request.Translations, cancellationToken);

            return _mapper.Map<UpdatedCongressSectionResponse>(updatedEntity);
        }

        private async Task UpsertTranslationsAsync(
            Guid rootId,
            IEnumerable<TranslationInputDto> translations,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ApplicationLanguageDto> activeLanguages =
                await _languageProvider.GetActiveLanguagesAsync(cancellationToken);

            HashSet<Guid> activeLanguageIds = activeLanguages
                .Select(language => language.Id)
                .ToHashSet();

            ApplicationLanguageDto defaultLanguage =
                await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

            List<CongressSectionTranslation> existingTranslations = _translationRepository.Query()
                .ToList()
                .Where(translation => translation.CongressSectionId == rootId)
                .ToList();

            foreach (TranslationInputDto input in translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId))
                    continue;

                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);

                if (!isDefaultLanguage && !hasAnyValue)
                    continue;

                CongressSectionTranslation? existingTranslation = existingTranslations
                    .FirstOrDefault(translation => translation.LanguageId == input.LanguageId);

                if (existingTranslation is null)
                {
                    CongressSectionTranslation translation = new();

                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "CongressSectionId", rootId);
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                    LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);

                    await _translationRepository.AddAsync(translation);
                    continue;
                }

                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(existingTranslation, TranslationFieldNames, input.Fields);
                await _translationRepository.UpdateAsync(existingTranslation);
            }
        }

        private async Task NormalizeVisibleOrdersAsync(
            CongressSection updatedEntity,
            int requestedOrder,
            CancellationToken cancellationToken)
        {
            List<CongressSection> entities = _repository.Query()
                .ToList()
                .Where(entity =>
                    entity.CongressId == updatedEntity.CongressId &&
                    entity.Id != updatedEntity.Id &&
                    !IsDeleted(entity))
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            int targetOrder = NormalizeTargetOrder(requestedOrder, entities.Count + 1);
            entities.Insert(targetOrder - 1, updatedEntity);

            await PersistNormalizedOrdersAsync(entities, cancellationToken);
        }

        private async Task PersistNormalizedOrdersAsync(
            IReadOnlyList<CongressSection> entities,
            CancellationToken cancellationToken)
        {
            for (int index = 0; index < entities.Count; index++)
            {
                int normalizedOrder = index + 1;

                if (entities[index].Order == normalizedOrder)
                    continue;

                entities[index].Order = normalizedOrder;
                await _repository.UpdateAsync(entities[index]);
            }
        }

        private static int NormalizeTargetOrder(int requestedOrder, int maxOrder)
        {
            if (requestedOrder <= 0)
                return maxOrder;

            return requestedOrder > maxOrder ? maxOrder : requestedOrder;
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}
