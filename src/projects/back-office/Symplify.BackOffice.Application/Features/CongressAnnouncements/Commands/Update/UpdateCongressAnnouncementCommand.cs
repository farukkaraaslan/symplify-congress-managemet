using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Constants;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.CongressAnnouncements.Commands.Update;

public class UpdateCongressAnnouncementCommand : IRequest<UpdatedCongressAnnouncementResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public CongressAnnouncementType Type { get; set; } = CongressAnnouncementType.General;
    public CongressAnnouncementStatus Status { get; set; } = CongressAnnouncementStatus.Draft;
    public DateTime? PublishStartDate { get; set; }
    public DateTime? PublishEndDate { get; set; }
    public bool IsPinned { get; set; }
    public bool ShowOnHomePage { get; set; } = true;
    public bool ShowInTicker { get; set; }
    public string? ExternalUrl { get; set; }
    public string? AttachmentPath { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<TranslationInputDto> Translations { get; set; } = new List<TranslationInputDto>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressAnnouncements";
    public string[] Roles => new[] { CongressAnnouncementsOperationClaims.Admin, CongressAnnouncementsOperationClaims.Write, CongressAnnouncementsOperationClaims.Update };

    public class Handler : IRequestHandler<UpdateCongressAnnouncementCommand, UpdatedCongressAnnouncementResponse>
    {
        private static readonly string[] TranslationFieldNames = { "Title", "Summary", "Content", "SeoTitle", "SeoDescription" };

        private readonly ICongressAnnouncementRepository _repository;
        private readonly ICongressAnnouncementTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly IMapper _mapper;
        private readonly CongressAnnouncementBusinessRules _rules;

        public Handler(
            ICongressAnnouncementRepository repository,
            ICongressAnnouncementTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider,
            IMapper mapper,
            CongressAnnouncementBusinessRules rules)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<UpdatedCongressAnnouncementResponse> Handle(
            UpdateCongressAnnouncementCommand request,
            CancellationToken cancellationToken)
        {
            request.ExternalUrl = Normalize(request.ExternalUrl);
            request.AttachmentPath = Normalize(request.AttachmentPath);

            await _rules.CongressShouldExist(request.CongressId, cancellationToken);
            await _rules.PublishDateRangeShouldBeValid(request.PublishStartDate, request.PublishEndDate);
            await _rules.OrderShouldBeValid(request.Order);
            await _rules.ExternalUrlShouldBeValid(request.ExternalUrl);
            await _rules.AttachmentPathShouldBeValid(request.AttachmentPath);
            await _rules.DefaultTranslationShouldExist(request.Translations, cancellationToken);
            await _rules.TranslationTitlesShouldBeValid(request.Translations, cancellationToken);

            CongressAnnouncement? entity = await _repository.GetAsync(predicate: item => item.Id == request.Id);
            await _rules.AnnouncementShouldExistWhenSelected(entity);
            await _rules.AnnouncementShouldBelongToCongress(entity!, request.CongressId);

            int requestedOrder = request.Order <= 0 ? entity!.Order : request.Order;

            entity!.CongressId = request.CongressId;
            entity.Type = request.Type;
            entity.Status = request.Status;
            entity.PublishStartDate = ToUtc(request.PublishStartDate);
            entity.PublishEndDate = ToUtc(request.PublishEndDate);
            entity.IsPinned = request.IsPinned;
            entity.ShowOnHomePage = request.ShowOnHomePage;
            entity.ShowInTicker = request.ShowInTicker;
            entity.ExternalUrl = request.ExternalUrl;
            entity.AttachmentPath = request.AttachmentPath;
            entity.Order = 0;
            entity.IsActive = request.IsActive;

            CongressAnnouncement savedEntity = await _repository.UpdateAsync(entity);

            await NormalizeVisibleOrdersAsync(savedEntity, requestedOrder, cancellationToken);
            await UpsertTranslationsAsync(savedEntity.Id, request.Translations, cancellationToken);

            return _mapper.Map<UpdatedCongressAnnouncementResponse>(savedEntity);
        }

        private async Task UpsertTranslationsAsync(
            Guid announcementId,
            IEnumerable<TranslationInputDto> translations,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ApplicationLanguageDto> activeLanguages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);
            HashSet<Guid> activeLanguageIds = activeLanguages.Select(language => language.Id).ToHashSet();
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            List<CongressAnnouncementTranslation> existingTranslations = _translationRepository.Query()
                .Where(translation => translation.CongressAnnouncementId == announcementId)
                .ToList();

            foreach (TranslationInputDto input in translations)
            {
                if (!activeLanguageIds.Contains(input.LanguageId))
                    continue;

                bool isDefaultLanguage = input.LanguageId == defaultLanguage.Id;
                bool hasAnyValue = LocalizedEntityRuntimeHelper.HasAnyValue(input.Fields, TranslationFieldNames);

                if (!isDefaultLanguage && !hasAnyValue)
                    continue;

                CongressAnnouncementTranslation? existingTranslation = existingTranslations
                    .FirstOrDefault(translation => translation.LanguageId == input.LanguageId);

                if (existingTranslation is null)
                {
                    CongressAnnouncementTranslation translation = new();
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "Id", Guid.NewGuid());
                    LocalizedEntityRuntimeHelper.SetPropertyValue(translation, "CongressAnnouncementId", announcementId);
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
            CongressAnnouncement changedEntity,
            int requestedOrder,
            CancellationToken cancellationToken)
        {
            List<CongressAnnouncement> entities = _repository.Query()
                .ToList()
                .Where(entity =>
                    entity.CongressId == changedEntity.CongressId &&
                    entity.Id != changedEntity.Id &&
                    !IsDeleted(entity))
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            int targetOrder = NormalizeTargetOrder(requestedOrder, entities.Count + 1);
            entities.Insert(targetOrder - 1, changedEntity);

            await PersistNormalizedOrdersAsync(entities, cancellationToken);
        }

        private async Task PersistNormalizedOrdersAsync(
            IReadOnlyList<CongressAnnouncement> entities,
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

        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static DateTime? ToUtc(DateTime? value)
        {
            if (!value.HasValue)
                return null;

            return value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            };
        }
    }
}
