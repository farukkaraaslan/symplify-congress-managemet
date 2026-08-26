using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Organization;

/// <summary>
/// Stores the SMTP sender configuration shared by every congress, registration,
/// submission and operational mail that belongs to one organization.
/// SMTP credentials are stored encrypted; plaintext passwords must never be persisted.
/// Mail logo objects are stored in private object storage and embedded into outgoing
/// messages as CID inline resources.
/// </summary>
public class OrganizationMailConfiguration : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid OrganizationId { get; set; }

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Versioned encrypted SMTP password payload.
    /// </summary>
    public string PasswordCipherText { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;

    public string? ReplyToEmail { get; set; }

    public string? ReplyToName { get; set; }

    /// <summary>
    /// Private MinIO bucket containing the mail logo.
    /// </summary>
    public string? MailLogoBucketName { get; set; }

    /// <summary>
    /// Private MinIO object key. This value is never rendered as a public URL.
    /// </summary>
    public string? MailLogoObjectName { get; set; }

    public string? MailLogoContentType { get; set; }

    public string? MailLogoFileName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastTestedAt { get; set; }

    public bool? LastTestSucceeded { get; set; }

    public string? LastTestError { get; set; }

    public virtual Organization Organization { get; set; } = null!;
}
