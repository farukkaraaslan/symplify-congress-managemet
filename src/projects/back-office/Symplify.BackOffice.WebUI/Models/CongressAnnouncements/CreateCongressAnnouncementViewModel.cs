using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.WebUI.Models.CongressAnnouncements;

public sealed class CreateCongressAnnouncementViewModel
{
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

    public List<CongressAnnouncementTranslationViewModel> Translations { get; set; } = new();
}
