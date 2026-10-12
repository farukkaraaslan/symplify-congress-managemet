namespace Symplify.BackOffice.WebUI.Models.ParticipationCertificates;

public sealed class PublicParticipationCertificateViewModel
{
    public int StatusCode { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool IsVerification { get; init; }
    public bool IsValid { get; init; }
    public Guid? PublicId { get; init; }
    public string VerificationCode { get; init; } = string.Empty;
    public string CongressName { get; init; } = string.Empty;
    public string SubmissionNumber { get; init; } = string.Empty;
    public string SubmissionTitle { get; init; } = string.Empty;
    public string ParticipantName { get; init; } = string.Empty;
    public string Culture { get; init; } = string.Empty;
    public DateTime? GeneratedAt { get; init; }
}
