namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class SubmissionFinalFilesResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new(
            "BackOffice.Submissions",
            "BackOffice.Submissions.FinalFiles.Filter.AllActiveCongresses",
            "Tüm aktif kongreler",
            "All active congresses"),

        new(
            "BackOffice.Submissions",
            "BackOffice.Submissions.FinalFiles.Filter.AllArchivedCongresses",
            "Tüm arşiv kongreleri",
            "All archived congresses"),

        new(
            "BackOffice.Submissions",
            "BackOffice.Submissions.FinalFiles.Action.ViewArchive",
            "Arşivi Görüntüle",
            "View Archive"),

        new(
            "BackOffice.Submissions",
            "BackOffice.Submissions.FinalFiles.Action.ViewActive",
            "Aktif Kongreleri Görüntüle",
            "View Active Congresses")
    };
}
