using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressAnnouncements.Queries.GetForUpdate;

public class GetCongressAnnouncementForUpdateQuery : IRequest<GetCongressAnnouncementForUpdateResponse>, ISecuredRequest
{
    public Guid Id { get; set; }

    public Guid CongressId { get; set; }

    public string[] Roles => new[] { CongressAnnouncementsOperationClaims.Admin, CongressAnnouncementsOperationClaims.Read };

    public class Handler : IRequestHandler<GetCongressAnnouncementForUpdateQuery, GetCongressAnnouncementForUpdateResponse>
    {
        private static readonly string[] TranslationFieldNames = { "Title", "Summary", "Content", "SeoTitle", "SeoDescription" };

        private readonly ICongressAnnouncementRepository _repository;
        private readonly ICongressAnnouncementTranslationRepository _translationRepository;
        private readonly IApplicationLanguageProvider _languageProvider;

        public Handler(
            ICongressAnnouncementRepository repository,
            ICongressAnnouncementTranslationRepository translationRepository,
            IApplicationLanguageProvider languageProvider)
        {
            _repository = repository;
            _translationRepository = translationRepository;
            _languageProvider = languageProvider;
        }

        public async Task<GetCongressAnnouncementForUpdateResponse> Handle(
            GetCongressAnnouncementForUpdateQuery request,
            CancellationToken cancellationToken)
        {
            CongressAnnouncement? entity = await _repository.GetAsync(predicate: item => item.Id == request.Id);

            if (entity is null || entity.CongressId != request.CongressId)
                throw new BusinessException(CongressAnnouncementsMessages.EntityNotFound);

            IReadOnlyList<ApplicationLanguageDto> languages = await _languageProvider.GetActiveLanguagesAsync(cancellationToken);

            List<CongressAnnouncementTranslation> translations = _translationRepository.Query()
                .Where(translation => translation.CongressAnnouncementId == request.Id)
                .ToList();

            return new GetCongressAnnouncementForUpdateResponse
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
                Translations = languages.Select(language =>
                {
                    CongressAnnouncementTranslation? translation = translations.FirstOrDefault(item => item.LanguageId == language.Id);

                    return new LocalizedTranslationDto
                    {
                        LanguageId = language.Id,
                        Culture = language.Culture,
                        LanguageName = language.Name,
                        IsDefault = language.IsDefault,
                        Exists = translation is not null,
                        Fields = LocalizedEntityRuntimeHelper.ExtractFields(translation, TranslationFieldNames)
                    };
                }).ToList()
            };
        }
    }
}
