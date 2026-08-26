namespace Symplify.BackOffice.Application.Features.CongressAnnouncements.Commands.DeleteTranslation;

public class DeletedCongressAnnouncementTranslationResponse
{
    public Guid Id { get; set; }
    public Guid CongressAnnouncementId { get; set; }
    public Guid LanguageId { get; set; }
}
