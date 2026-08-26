namespace Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Queries.GetByOrganizationId;

public sealed class GetOrganizationMailConfigurationByOrganizationIdResponse
{
    public Guid? Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string? ReplyToEmail { get; set; }
    public string? ReplyToName { get; set; }
    public string? MailLogoBucketName { get; set; }
    public string? MailLogoObjectName { get; set; }
    public string? MailLogoContentType { get; set; }
    public string? MailLogoFileName { get; set; }
    public bool HasMailLogo { get; set; }
    public bool IsActive { get; set; } = true;
    public bool HasStoredPassword { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public bool? LastTestSucceeded { get; set; }
    public string? LastTestError { get; set; }
    public bool Exists { get; set; }
}
