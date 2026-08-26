namespace Symplify.BackOffice.Application.Services.Mailing;

public sealed class ResolvedOrganizationMailConfiguration
{
    public Guid OrganizationId { get; init; }
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public bool EnableSsl { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;
    public string? ReplyToEmail { get; init; }
    public string? ReplyToName { get; init; }
    public string? MailLogoBucketName { get; init; }
    public string? MailLogoObjectName { get; init; }
    public string? MailLogoContentType { get; init; }
    public string? MailLogoFileName { get; init; }
}
