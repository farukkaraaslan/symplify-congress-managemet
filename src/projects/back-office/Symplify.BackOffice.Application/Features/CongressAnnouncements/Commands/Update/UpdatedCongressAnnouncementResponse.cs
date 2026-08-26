namespace Symplify.BackOffice.Application.Features.CongressAnnouncements.Commands.Update;

public class UpdatedCongressAnnouncementResponse
{

    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Symplify.BackOffice.Domain.Enums.CongressAnnouncementType Type { get; set; }
    public Symplify.BackOffice.Domain.Enums.CongressAnnouncementStatus Status { get; set; }
    public DateTime? PublishStartDate { get; set; }
    public DateTime? PublishEndDate { get; set; }
    public bool IsPinned { get; set; }
    public bool ShowOnHomePage { get; set; }
    public bool ShowInTicker { get; set; }
    public string? ExternalUrl { get; set; }
    public string? AttachmentPath { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
