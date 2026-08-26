namespace Symplify.BackOffice.Infrastructure.Email;

public sealed class MailCredentialProtectionOptions
{
    public const string SectionName = "Mail:CredentialProtection";

    /// <summary>
    /// Base64-encoded 32-byte AES key. Supply it through an environment variable or secret store.
    /// </summary>
    public string Key { get; set; } = string.Empty;
}
