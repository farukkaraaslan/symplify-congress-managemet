namespace Symplify.BackOffice.Application.Services.Workflow;

public interface IAcceptanceLetterPdfRenderer
{
    byte[] Render(AcceptanceLetterPdfModel model);
}

public sealed class AcceptanceLetterPdfModel
{
    public string OrganizationShortName { get; init; } = string.Empty;
    public string OrganizationName { get; init; } = string.Empty;
    public string OrganizationEmail { get; init; } = string.Empty;
    public string CongressTitle { get; init; } = string.Empty;
    public string CongressLocation { get; init; } = string.Empty;
    public string CongressDateRange { get; init; } = string.Empty;
    public string SubmissionCode { get; init; } = string.Empty;
    public string AuthorFullName { get; init; } = string.Empty;
    public string SubmissionTitle { get; init; } = string.Empty;
    public string SubmissionTypeName { get; init; } = string.Empty;
    public string BodyContent { get; init; } = string.Empty;
    public string SignerFullName { get; init; } = string.Empty;
    public string SignerDuty { get; init; } = string.Empty;
    public string VerificationCode { get; init; } = string.Empty;
    public string VerificationUrl { get; init; } = string.Empty;
    public byte[]? LogoBytes { get; init; }
    public byte[]? SignatureBytes { get; init; }
    public byte[]? QrCodeBytes { get; init; }
    public string Culture { get; init; } = "en-US";
}
