using Microsoft.AspNetCore.Mvc.Rendering;
using Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;

namespace Symplify.BackOffice.WebUI.Models.ParticipationCertificates;

public sealed class ParticipationCertificatesIndexViewModel
{
    public Guid CongressId { get; set; }
    public string CongressTitle { get; set; } = string.Empty;
    public string? SubmissionStatusCode { get; set; }
    public string? PaymentStatusCode { get; set; }
    public string CertificateCulture { get; set; } = ParticipationCertificateCultures.Turkish;
    public string DefaultCertificateCulture { get; set; } = ParticipationCertificateCultures.Turkish;
    public string? CandidateSearch { get; set; }
    public int CandidatePage { get; set; } = 1;
    public int CandidatePageSize { get; set; } = 100;
    public int CandidateTotalPages { get; set; } = 1;
    public IReadOnlyList<SelectListItem> CongressOptions { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> SubmissionStatusOptions { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> PaymentStatusOptions { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> CertificateCultureOptions { get; set; } = Array.Empty<SelectListItem>();
    public ParticipationCertificateTemplateDto? Template { get; set; }
    public IReadOnlyList<ParticipationCertificateTemplateDto> Templates { get; set; } = Array.Empty<ParticipationCertificateTemplateDto>();
    public IReadOnlyList<ParticipationCertificateCandidateDto> Candidates { get; set; } = Array.Empty<ParticipationCertificateCandidateDto>();
    public ParticipationCertificateGenerationJobDto? GenerationJob { get; set; }
    public bool HasCongressOptions => CongressOptions.Count > 0;
    public int CandidateCount { get; set; }
    public int EligibleCandidateCount { get; set; }
    public int GeneratedCount { get; set; }
    public int EmailQueuedCount { get; set; }
    public int EmailSentCount { get; set; }
    public int RevokedCount { get; set; }
    public int MissingEmailCount { get; set; }
    public int MailSelectableCount { get; set; }
}
