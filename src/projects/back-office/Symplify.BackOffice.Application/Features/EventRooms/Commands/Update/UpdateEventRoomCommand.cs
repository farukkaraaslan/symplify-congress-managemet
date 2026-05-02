using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.EventRooms.Constants;
using Symplify.BackOffice.Application.Features.EventRooms.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.EventRooms.Commands.Update;

public class UpdateEventRoomCommand : IRequest<UpdatedEventRoomResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public string? Code { get; set; }

    /// <summary>
    /// 0 veya negatif gönderilirse mevcut sıra korunur.
    /// Pozitif gönderilirse kayıt hedef sıraya taşınır ve aktif liste normalize edilir.
    /// </summary>
    public int Order { get; set; }

    public bool IsActive { get; set; }

    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetEventRooms";

    public string[] Roles => new[] { EventRoomsOperationClaims.Admin, EventRoomsOperationClaims.Write, EventRoomsOperationClaims.Update };

    public class UpdateEventRoomCommandHandler : IRequestHandler<UpdateEventRoomCommand, UpdatedEventRoomResponse>
    {
        private static readonly string[] TranslationFieldNames = new[] { "Name", "Description" };

        private readonly IEventRoomRepository _repository;
        private readonly IEventRoomTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly EventRoomBusinessRules _rules;

        public UpdateEventRoomCommandHandler(
            IEventRoomRepository repository,
            IEventRoomTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            IMapper mapper,
            EventRoomBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<UpdatedEventRoomResponse> Handle(UpdateEventRoomCommand request, CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);

            EventRoom? entity = await _repository.GetAsync(predicate: item => item.Id.Equals(request.Id));
            await _rules.EventRoomShouldExistWhenSelected(entity);

            entity!.Code = request.Code;
            entity.IsActive = request.IsActive;

            await NormalizeVisibleOrdersAsync(entity, request.Order, cancellationToken);

            EventRoom updatedEntity = await _repository.UpdateAsync(entity);
            await UpsertTranslationsAsync(request.Id, request.Translations, cancellationToken);

            return _mapper.Map<UpdatedEventRoomResponse>(updatedEntity);
        }

        private async Task UpsertTranslationsAsync(
            Guid rootId,
            IEnumerable<TranslationInputDto> translations,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(language => language.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);

            List<EventRoomTranslation> existingTranslations = _translationRepository.Query()
                .ToList()
                .Where(translation => EqualityComparer<Guid>.Default.Equals(translation.EventRoomId, rootId) && !IsDeleted(translation))
                .ToList();

            foreach (TranslationInputDto input in translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId))
                    continue;

                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);

                if (!isDefaultLanguage && !hasAnyValue)
                    continue;

                EventRoomTranslation? existingTranslation = existingTranslations
                    .FirstOrDefault(translation => translation.LanguageId == input.LanguageId);

                if (existingTranslation is null)
                {
                    EventRoomTranslation translation = new();
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "EventRoomId", rootId);
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
            EventRoom currentEntity,
            int requestedOrder,
            CancellationToken cancellationToken)
        {
            List<EventRoom> entities = _repository.Query()
                .ToList()
                .Where(entity => !IsDeleted(entity) && entity.Id != currentEntity.Id)
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            int targetOrder = requestedOrder > 0
                ? NormalizeTargetOrder(requestedOrder, entities.Count + 1)
                : NormalizeTargetOrder(currentEntity.Order, entities.Count + 1);

            entities.Insert(targetOrder - 1, currentEntity);

            await PersistNormalizedOrdersAsync(entities, cancellationToken);
        }

        private async Task PersistNormalizedOrdersAsync(
            IReadOnlyList<EventRoom> entities,
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

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}
