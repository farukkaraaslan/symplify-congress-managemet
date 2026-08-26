namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class SubmissionFinalFileShortLinkResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new("BackOffice.Submissions", "BackOffice.Submissions.FinalFiles.Action.ShortLink", "Kısa Link", "Short Link"),
        new("BackOffice.Submissions", "BackOffice.Submissions.FinalFiles.Action.ShortLinks", "Kısa Linkleri Al", "Get Short Links"),
        new("BackOffice.Submissions", "BackOffice.Submissions.FinalFiles.Message.ShortLinkCreated", "Kısa link oluşturuldu.", "Short link has been created."),
        new("BackOffice.Submissions", "BackOffice.Submissions.FinalFiles.Message.ShortLinkUnavailable", "Kısa link oluşturulabilecek onaylı dosya bulunamadı.", "No approved file is available to create a short link."),
        new("BackOffice.Submissions", "BackOffice.Submissions.FinalFiles.Message.ApprovalRequiredForShortLink", "Kısa link oluşturmak için dosya onaylı olmalıdır. Video dosyaları ayrıca program kitabına eklenmelidir.", "The file must be approved to create a short link. Video files must also be included in the program book.")
    };
}
