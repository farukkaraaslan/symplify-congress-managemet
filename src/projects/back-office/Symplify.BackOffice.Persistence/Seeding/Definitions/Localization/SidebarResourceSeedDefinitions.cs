namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class SidebarResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new("BackOffice.Sidebar", "BackOffice.Sidebar.Dashboard", "Panel", "Dashboard"),

        new("BackOffice.Sidebar", "BackOffice.Sidebar.Lookups", "Tanımlar", "Lookups"),
        new("BackOffice.Sidebar", "BackOffice.Sidebar.Topics", "Konular", "Topics"),
        new("BackOffice.Sidebar", "BackOffice.Sidebar.Titles", "Ünvanlar", "Titles"),
        new("BackOffice.Sidebar", "BackOffice.Sidebar.DocumentTypes", "Doküman Tipleri", "Document Types"),
        new("BackOffice.Sidebar", "BackOffice.Sidebar.SubmissionTypes", "Bildiri Tipleri", "Submission Types"),
        new("BackOffice.Sidebar", "BackOffice.Sidebar.EventRooms", "Etkinlik Salonları", "Event Rooms"),
        new("BackOffice.Sidebar", "BackOffice.Sidebar.EvaluationCriteria", "Değerlendirme Kriterleri", "Evaluation Criteria"),

        new("BackOffice.Sidebar", "BackOffice.Sidebar.OrganizationManagement", "Organizasyon Yönetimi", "Organization Management"),
        new("BackOffice.Sidebar", "BackOffice.Sidebar.Organizations", "Organizasyonlar", "Organizations"),
        new("BackOffice.Sidebar", "BackOffice.Sidebar.OrganizationApiKeys", "Organizasyon API Anahtarları", "Organization API Keys"),
        new("BackOffice.Sidebar", "BackOffice.Sidebar.OrganizationUsers", "Organizasyon Kullanıcıları", "Organization Users")
    };
}
