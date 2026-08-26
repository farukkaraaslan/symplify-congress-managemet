using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.CongressAnnouncements.Queries.GetForUpdate;

public class GetCongressAnnouncementForUpdateResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public CongressAnnouncementType Type { get; set; }
    public CongressAnnouncementStatus Status { get; set; }
    public DateTime? PublishStartDate { get; set; }
    public DateTime? PublishEndDate { get; set; }
    public bool IsPinned { get; set; }
    public bool ShowOnHomePage { get; set; }
    public bool ShowInTicker { get; set; }
    public string? ExternalUrl { get; set; }
    public string? AttachmentPath { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public List<LocalizedTranslationDto> Translations { get; set; } = new();
}
