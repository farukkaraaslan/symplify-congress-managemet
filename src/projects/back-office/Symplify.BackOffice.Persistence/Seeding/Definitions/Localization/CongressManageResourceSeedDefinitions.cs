namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class CongressManageResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.PageTitle", "Kongre Yönetimi", "Congress Management"),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.InfoTitle", "Kongre yönetimi", "Congress management"),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.InfoDescription", "Seçili kongreye ait slider, genel bilgiler, duyurular, kurullar, tarihler, ödeme planları, dokümanlar, workflow, konular, bildiri türleri ve değerlendirme kriterlerini bu ekrandan yönetin.", "Manage sliders, general information, announcements, boards, dates, payment plans, documents, workflow, topics, submission types and evaluation criteria for the selected congress on this screen."),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.ListsTitle", "Kongre Yönetim Listeleri", "Congress Management Lists"),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.ListsDescription", "Kayıtlar sekmeler halinde listelenir. Yeni kayıtlar modal üzerinden yönetilir.", "Records are listed in tabs. New records are managed through modals."),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.CongressInfoTitle", "Kongre Bilgileri", "Congress Information"),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.CongressInfoDescription", "Aktif kongre kaydının genel görünümü.", "General overview of the selected congress record."),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.PlaceholderDescription", "Bu sekmenin CRUD entegrasyonu sonraki adımda eklenecek.", "CRUD integration for this tab will be added in the next step."),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.Tabs.Congress", "Kongre", "Congress"),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.Tabs.Slider", "Slider", "Slider"),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.Tabs.Sections", "Genel Bilgiler", "General Information"),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.Tabs.Announcements", "Duyurular", "Announcements"),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.Tabs.Boards", "Kurullar", "Boards"),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.Tabs.ImportantDates", "Önemli Tarihler", "Important Dates"),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.Tabs.PaymentPlans", "Ödeme Planları", "Payment Plans"),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.Tabs.Documents", "Dokümanlar", "Documents"),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.Tabs.Workflow", "Workflow", "Workflow"),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.Tabs.Topics", "Konular", "Topics"),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.Tabs.SubmissionTypes", "Bildiri Türleri", "Submission Types"),
        new("BackOffice.Congresses", "BackOffice.Congresses.Manage.Tabs.EvaluationCriteria", "Değerlendirme Kriterleri", "Evaluation Criteria"),
        new("BackOffice.Congresses", "BackOffice.Congresses.Fields.Description", "Açıklama", "Description")
    };
}
