using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Constants;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressAnnouncements.Commands.DeleteTranslation;

public class DeleteCongressAnnouncementTranslationCommand : IRequest<DeletedCongressAnnouncementTranslationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressAnnouncementId { get; set; }
    public Guid LanguageId { get; set; }

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressAnnouncements";
    public string[] Roles => new[] { CongressAnnouncementsOperationClaims.Admin, CongressAnnouncementsOperationClaims.Write, CongressAnnouncementsOperationClaims.Delete };

    public class Handler : IRequestHandler<DeleteCongressAnnouncementTranslationCommand, DeletedCongressAnnouncementTranslationResponse>
    {
        private readonly ICongressAnnouncementTranslationRepository _translationRepository;
        private readonly CongressAnnouncementBusinessRules _rules;

        public Handler(
            ICongressAnnouncementTranslationRepository translationRepository,
            CongressAnnouncementBusinessRules rules)
        {
            _translationRepository = translationRepository;
            _rules = rules;
        }

        public async Task<DeletedCongressAnnouncementTranslationResponse> Handle(
            DeleteCongressAnnouncementTranslationCommand request,
            CancellationToken cancellationToken)
        {
            await _rules.DefaultTranslationCannotBeDeleted(request.LanguageId, cancellationToken);

            CongressAnnouncementTranslation? translation = _translationRepository.Query()
                .FirstOrDefault(item =>
                    item.CongressAnnouncementId == request.CongressAnnouncementId &&
                    item.LanguageId == request.LanguageId);

            await _rules.TranslationShouldExistWhenSelected(translation);

            CongressAnnouncementTranslation deletedTranslation = await _translationRepository.DeleteAsync(translation!);

            return new DeletedCongressAnnouncementTranslationResponse
            {
                Id = deletedTranslation.Id,
                CongressAnnouncementId = deletedTranslation.CongressAnnouncementId,
                LanguageId = deletedTranslation.LanguageId
            };
        }
    }
}
