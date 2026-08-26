using Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;

namespace Symplify.BackOffice.WebUI.Models.ParticipationCertificates;

public sealed class ParticipationCertificateTemplateSettingsPartialModel
{
    public Guid CongressId { get; init; }
    public string CurrentCulture { get; init; } = "tr-TR";
    public string TemplateCulture { get; init; } = ParticipationCertificateCultures.Turkish;
    public string CultureLabel { get; init; } = string.Empty;
    public ParticipationCertificateTemplateDto? Template { get; init; }
    public string DefaultBodyText { get; init; } = string.Empty;
    public string DefaultMailSubject { get; init; } = string.Empty;
    public string DefaultMailTitle { get; init; } = string.Empty;
    public string DefaultMailBodyHtml { get; init; } = string.Empty;
}
