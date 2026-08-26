using Core.Persistence.Repositories;
namespace Symplify.BackOffice.Domain.Submission;

public sealed class ParticipationCertificateTemplate : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }

    public string Name { get; set; } = "Katılım Belgesi";

    public string Culture { get; set; } = "tr-TR";

    public bool IsDefault { get; set; }

    public string? BodyText { get; set; }

    public string? MailSubject { get; set; }

    public string? MailTitle { get; set; }

    public string? MailBodyHtml { get; set; }

    public bool IsActive { get; set; } = true;

    public string? StorageProvider { get; set; }

    public string BucketName { get; set; } = string.Empty;

    public string ObjectName { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/pdf";

    public long? FileSize { get; set; }

    public string? ETag { get; set; }

    /// <summary>
    /// PDF coordinate system: bottom-left origin. Defaults target the UBAK A4 landscape sample.
    /// </summary>
    public float NameBoxX { get; set; } = 120f;

    public float NameBoxY { get; set; } = 275f;

    public float NameBoxWidth { get; set; } = 600f;

    public float NameBoxHeight { get; set; } = 70f;

    public float NameFontSize { get; set; } = 20f;

    public string NameFontColorHex { get; set; } = "#FFFFFF";

    public bool CoverPlaceholderBackground { get; set; } = false;

    public string PlaceholderBackgroundColorHex { get; set; } = "#06142E";

    public bool RenderCommitteeSignature { get; set; } = true;

    /// <summary>
    /// PDF coordinate system: bottom-left origin. Defaults target the right-bottom organizing committee signature area.
    /// </summary>
    public float CommitteeSignatureBoxX { get; set; } = 515f;

    public float CommitteeSignatureBoxY { get; set; } = 112f;

    public float CommitteeSignatureBoxWidth { get; set; } = 135f;

    public float CommitteeSignatureBoxHeight { get; set; } = 55f;

    public DateTime UploadedAt { get; set; }

    public Symplify.BackOffice.Domain.Congress.Congress Congress { get; set; } = null!;
}
