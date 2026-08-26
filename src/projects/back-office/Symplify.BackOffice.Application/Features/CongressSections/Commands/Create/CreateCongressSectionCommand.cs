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

namespace Symplify.BackOffice.Application.Features.CongressSections.Commands.Create;

public class CreateCongressSectionCommand
    : IRequest<CreatedCongressSectionResponse>, ISecuredRequest, ICacheRemoverRequest
{
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
        CongressSectionsOperationClaims.Add
    };

    public class CreateCongressSectionCommandHandler
        : IRequestHandler<CreateCongressSectionCommand, CreatedCongressSectionResponse>
    {
        private static readonly string[] TranslationFieldNames = { "Title", "Content" };

        private readonly ICongressSectionRepository _repository;
        private readonly ICongressSectionTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly CongressSectionBusinessRules _rules;

        public CreateCongressSectionCommandHandler(
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

        public async Task<CreatedCongressSectionResponse> Handle(
            CreateCongressSectionCommand request,
            CancellationToken cancellationToken)
        {
            request.BindingKey = (request.BindingKey ?? string.Empty).Trim();

            await _rules.CongressShouldExist(request.CongressId, cancellationToken);
            await _rules.BindingKeyShouldBeValid(request.BindingKey);
            await _rules.BindingKeyShouldBeUniqueForCongress(request.CongressId, request.BindingKey);
            await _rules.OrderShouldBeValid(request.Order);
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            await _rules.TranslationTitlesShouldBeValid(request.Translations, cancellationToken);

            CongressSection entity = new()
            {
                Id = Guid.NewGuid(),
                CongressId = request.CongressId,
                BindingKey = request.BindingKey,
                Order = 0,
                IsActive = request.IsActive
            };

            CongressSection createdEntity = await _repository.AddAsync(entity);

            await NormalizeVisibleOrdersAsync(createdEntity, request.Order, cancellationToken);
            await CreateTranslationsAsync(createdEntity.Id, request.Translations, cancellationToken);

            return _mapper.Map<CreatedCongressSectionResponse>(createdEntity);
        }

        private async Task CreateTranslationsAsync(
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

            foreach (TranslationInputDto input in translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId))
                    continue;

                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);

                if (!isDefaultLanguage && !hasAnyValue)
                    continue;

                CongressSectionTranslation translation = new();

                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "CongressSectionId", rootId);
                LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "LanguageId", input.LanguageId);
                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(translation, TranslationFieldNames, input.Fields);

                await _translationRepository.AddAsync(translation);
            }
        }

        private async Task NormalizeVisibleOrdersAsync(
            CongressSection createdEntity,
            int requestedOrder,
            CancellationToken cancellationToken)
        {
            List<CongressSection> entities = _repository.Query()
                .ToList()
                .Where(entity =>
                    entity.CongressId == createdEntity.CongressId &&
                    entity.Id != createdEntity.Id &&
                    !IsDeleted(entity))
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            int targetOrder = NormalizeTargetOrder(requestedOrder, entities.Count + 1);
            entities.Insert(targetOrder - 1, createdEntity);

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
