using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressAnnouncements.Queries.GetById;

public class GetByIdCongressAnnouncementQuery : IRequest<GetByIdCongressAnnouncementResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }

    public string[] Roles => new[] { CongressAnnouncementsOperationClaims.Admin, CongressAnnouncementsOperationClaims.Read };

    public class Handler : IRequestHandler<GetByIdCongressAnnouncementQuery, GetByIdCongressAnnouncementResponse>
    {
        private readonly ICongressAnnouncementRepository _repository;
        private readonly ICongressAnnouncementTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;

        public Handler(ICongressAnnouncementRepository repository, ICongressAnnouncementTranslationRepository translationRepository, IApplicationLanguageProvider languageProvider, ICurrentLanguageProvider currentLanguageProvider, ITranslationFallbackResolver fallbackResolver)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
        }

        public async Task<GetByIdCongressAnnouncementResponse> Handle(GetByIdCongressAnnouncementQuery request, CancellationToken cancellationToken)
        {
            CongressAnnouncement? entity = await _repository.GetAsync(predicate: item => item.Id == request.Id);
            if (entity is null)
                throw new BusinessException(CongressAnnouncementsMessages.EntityNotFound);

            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);
            List<CongressAnnouncementTranslation> translations = _translationRepository.Query().Where(translation => translation.CongressAnnouncementId == request.Id).ToList();
            CongressAnnouncementTranslation? requestedTranslation = translations.FirstOrDefault(translation => translation.LanguageId == requestedLanguage.Id);
            CongressAnnouncementTranslation? displayTranslation = _fallbackResolver.Resolve(translations, requestedLanguage.Id, defaultLanguage.Id);

            return Project(entity, displayTranslation, requestedTranslation is null && displayTranslation is not null);
        }

        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(Guid? languageId, string? culture, ApplicationLanguageDto defaultLanguage, CancellationToken cancellationToken)
        {
            if (languageId.HasValue) return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;
            if (!string.IsNullOrWhiteSpace(culture)) return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;
            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }

        private static GetByIdCongressAnnouncementResponse Project(CongressAnnouncement entity, CongressAnnouncementTranslation? translation, bool isFallback)
        {
            return new GetByIdCongressAnnouncementResponse
            {
                Id = entity.Id,
                CongressId = entity.CongressId,
                Type = entity.Type,
                Status = entity.Status,
                PublishStartDate = entity.PublishStartDate,
                PublishEndDate = entity.PublishEndDate,
                IsPinned = entity.IsPinned,
                ShowOnHomePage = entity.ShowOnHomePage,
                ShowInTicker = entity.ShowInTicker,
                ExternalUrl = entity.ExternalUrl,
                AttachmentPath = entity.AttachmentPath,
                Order = entity.Order,
                IsActive = entity.IsActive,
                IsCurrentlyPublished = IsCurrentlyPublished(entity, DateTime.UtcNow),
                Title = translation?.Title ?? string.Empty,
                Summary = translation?.Summary,
                Content = translation?.Content,
                SeoTitle = translation?.SeoTitle,
                SeoDescription = translation?.SeoDescription,
                DisplayLanguageId = translation?.LanguageId ?? default,
                IsFallback = isFallback
            };
        }

        private static bool IsCurrentlyPublished(CongressAnnouncement entity, DateTime utcNow)
            => entity.IsActive &&
               entity.Status == Symplify.BackOffice.Domain.Enums.CongressAnnouncementStatus.Published &&
               (!entity.PublishStartDate.HasValue || entity.PublishStartDate.Value <= utcNow) &&
               (!entity.PublishEndDate.HasValue || entity.PublishEndDate.Value >= utcNow);
    }
}
