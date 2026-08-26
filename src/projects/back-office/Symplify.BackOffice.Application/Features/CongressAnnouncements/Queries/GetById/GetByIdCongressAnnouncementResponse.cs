namespace Symplify.BackOffice.Application.Features.CongressAnnouncements.Queries.GetById;

public class GetByIdCongressAnnouncementResponse
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
    public bool IsCurrentlyPublished { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public Guid DisplayLanguageId { get; set; }
    public bool IsFallback { get; set; }
}
