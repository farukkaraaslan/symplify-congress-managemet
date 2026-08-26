using Symplify.Portal.WebUI.Models.PublicSite;

namespace Symplify.Portal.WebUI.Models.Pages;

public sealed class HomeIndexViewModel
{
    public PublicHomeResponse Home { get; set; } = new();

    public IReadOnlyCollection<PublicAnnouncementResponse> TickerAnnouncements =>
        OrderAnnouncements(Home.Announcements.Where(x => x.ShowInTicker)).Take(5).ToArray();

    public IReadOnlyCollection<PublicAnnouncementResponse> FeaturedAnnouncements =>
        OrderAnnouncements(Home.Announcements).Take(4).ToArray();

    private static IOrderedEnumerable<PublicAnnouncementResponse> OrderAnnouncements(IEnumerable<PublicAnnouncementResponse> announcements) =>
        announcements
            .OrderByDescending(x => x.IsPinned)
            .ThenByDescending(x => x.PublishStartDate ?? DateTime.MinValue)
            .ThenBy(x => x.Order)
            .ThenBy(x => x.Title);
}

public sealed class BoardsIndexViewModel
{
    public PublicBoardsResponse Data { get; set; } = new();
}

public sealed class DocumentsIndexViewModel
{
    public PublicDocumentsResponse Data { get; set; } = new();
}

public sealed class ContactIndexViewModel
{
    public PublicContactResponse Data { get; set; } = new();
}

public sealed class ContentsIndexViewModel
{
    public PublicContentsResponse Data { get; set; } = new();
}

public sealed class SectionsIndexViewModel
{
    public PublicSectionsResponse Data { get; set; } = new();
    public PublicContentsResponse Contents { get; set; } = new();
}

public sealed class PaymentIndexViewModel
{
    public string PageTitle { get; set; } = string.Empty;
    public PublicSectionResponse? Section { get; set; }
}

public sealed class SectionDetailViewModel
{
    public PublicSectionResponse Section { get; set; } = new();
}
