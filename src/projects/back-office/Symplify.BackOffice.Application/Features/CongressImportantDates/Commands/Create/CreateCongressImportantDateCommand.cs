using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Constants;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Create;

public class CreateCongressImportantDateCommand
    : IRequest<CreatedCongressImportantDateResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>
    /// 0 veya negatif gönderilirse kayıt listenin sonuna eklenir.
    /// Pozitif gönderilirse hedef sıraya yerleştirilir ve kongreye ait liste normalize edilir.
    /// </summary>
    public int Order { get; set; }

    public bool IsActive { get; set; }

    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetCongressImportantDates";

    public string[] Roles => new[]
    {
        CongressImportantDatesOperationClaims.Admin,
        CongressImportantDatesOperationClaims.Write,
        CongressImportantDatesOperationClaims.Add
    };

    public class CreateCongressImportantDateCommandHandler
        : IRequestHandler<CreateCongressImportantDateCommand, CreatedCongressImportantDateResponse>
    {
        private static readonly string[] TranslationFieldNames =
        {
            "Title",
            "Description"
        };

        private readonly ICongressImportantDateRepository _repository;
        private readonly ICongressImportantDateTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly CongressImportantDateBusinessRules _rules;

        public CreateCongressImportantDateCommandHandler(
            ICongressImportantDateRepository repository,
            ICongressImportantDateTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            IMapper mapper,
            CongressImportantDateBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<CreatedCongressImportantDateResponse> Handle(
            CreateCongressImportantDateCommand request,
            CancellationToken cancellationToken)
        {
            DateTime startDateUtc = ConvertToUtc(request.StartDate);
            DateTime endDateUtc = ConvertToUtc(request.EndDate);

            await _rules.CongressShouldExist(request.CongressId, cancellationToken);
            await _rules.DateRangeShouldBeValid(startDateUtc, endDateUtc);
            await _rules.OrderShouldBeValid(request.Order);
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            await _rules.TranslationTitlesShouldBeValid(request.Translations, cancellationToken);

            CongressImportantDate entity = new()
            {
                Id = Guid.NewGuid(),
                CongressId = request.CongressId,
                StartDate = startDateUtc,
                EndDate = endDateUtc,
                Order = 0,
                IsActive = request.IsActive
            };

            CongressImportantDate createdEntity = await _repository.AddAsync(entity);

            await NormalizeVisibleOrdersAsync(createdEntity, request.Order, cancellationToken);
            await CreateTranslationsAsync(createdEntity.Id, request.Translations, cancellationToken);

            return _mapper.Map<CreatedCongressImportantDateResponse>(createdEntity);
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

                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(
                    input.Fields,
                    TranslationFieldNames);

                if (!isDefaultLanguage && !hasAnyValue)
                    continue;

                CongressImportantDateTranslation translation = new();

                LocalizedEntityRuntimeHelper.SetPropertyValue(
                    translation,
                    "Id",
                    Guid.NewGuid());

                LocalizedEntityRuntimeHelper.SetPropertyValue(
                    translation,
                    "CongressImportantDateId",
                    rootId);

                LocalizedEntityRuntimeHelper.SetPropertyValue(
                    translation,
                    "LanguageId",
                    input.LanguageId);

                LocalizedEntityRuntimeHelper.ApplyFieldDictionary(
                    translation,
                    TranslationFieldNames,
                    input.Fields);

                await _translationRepository.AddAsync(translation);
            }
        }

        private async Task NormalizeVisibleOrdersAsync(
            CongressImportantDate createdEntity,
            int requestedOrder,
            CancellationToken cancellationToken)
        {
            List<CongressImportantDate> entities = _repository
                .Query()
                .ToList()
                .Where(entity =>
                    entity.CongressId == createdEntity.CongressId &&
                    !IsDeleted(entity) &&
                    entity.Id != createdEntity.Id)
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.StartDate)
                .ThenBy(entity => entity.EndDate)
                .ThenBy(entity => entity.Id)
                .ToList();

            int targetOrder = NormalizeTargetOrder(requestedOrder, entities.Count + 1);

            entities.Insert(targetOrder - 1, createdEntity);

            await PersistNormalizedOrdersAsync(entities, cancellationToken);
        }

        private async Task PersistNormalizedOrdersAsync(
            IReadOnlyList<CongressImportantDate> entities,
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

            return requestedOrder > maxOrder
                ? maxOrder
                : requestedOrder;
        }

        private static DateTime ConvertToUtc(DateTime value)
        {
            if (value == default)
                return value;

            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime(),
                _ => value
            };
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(
                entity,
                "DeletedDate");

            return deletedDate is not null;
        }
    }
}