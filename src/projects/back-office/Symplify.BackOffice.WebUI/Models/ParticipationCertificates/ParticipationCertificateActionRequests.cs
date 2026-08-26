namespace Symplify.BackOffice.WebUI.Models.ParticipationCertificates;

public sealed class ParticipationCertificateGenerateRequest
{
    public Guid CongressId { get; set; }
    public string? CertificateCulture { get; set; }
    public string? SubmissionStatusCode { get; set; }
    public string? PaymentStatusCode { get; set; }
    public string? CandidateSearch { get; set; }
    public bool SelectAllFiltered { get; set; }
    public List<string> SelectedCandidateKeys { get; set; } = new();
    public List<string> ExcludedCandidateKeys { get; set; } = new();
}

public sealed class ParticipationCertificateEmailRequest
{
    public Guid CongressId { get; set; }
    public string? CertificateCulture { get; set; }
    public string? EmailStatus { get; set; }
    public string? SearchText { get; set; }
    public bool SelectAllFiltered { get; set; }
    public List<Guid> CertificateIds { get; set; } = new();
    public List<Guid> ExcludedCertificateIds { get; set; } = new();
}

public sealed class ParticipationCertificateRevokeRequest
{
    public Guid CertificateId { get; set; }
    public string? Reason { get; set; }
}
