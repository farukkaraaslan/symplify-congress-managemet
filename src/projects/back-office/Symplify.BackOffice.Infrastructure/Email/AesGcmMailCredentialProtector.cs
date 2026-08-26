using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Services.Email;

namespace Symplify.BackOffice.Infrastructure.Email;

public sealed class AesGcmMailCredentialProtector : IMailCredentialProtector
{
    private const byte PayloadVersion = 1;
    private const int NonceLength = 12;
    private const int TagLength = 16;
    private static readonly byte[] AssociatedData = Encoding.UTF8.GetBytes(
        "Symplify.BackOffice.CongressMailConfiguration.v1");

    private readonly byte[] _key;

    public AesGcmMailCredentialProtector(IOptions<MailCredentialProtectionOptions> options)
    {
        string configuredKey = options.Value.Key?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            throw new InvalidOperationException(
                "Mail:CredentialProtection:Key is required. Configure a Base64 encoded 32-byte key.");
        }

        try
        {
            _key = Convert.FromBase64String(configuredKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "Mail:CredentialProtection:Key must be valid Base64.",
                exception);
        }

        if (_key.Length != 32)
        {
            throw new InvalidOperationException(
                "Mail:CredentialProtection:Key must decode to exactly 32 bytes.");
        }
    }

    public string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("SMTP password cannot be empty.", nameof(plainText));

        byte[] plaintext = Encoding.UTF8.GetBytes(plainText);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceLength);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagLength];

        using AesGcm aes = new(_key, TagLength);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, AssociatedData);

        byte[] payload = new byte[1 + NonceLength + TagLength + ciphertext.Length];
        payload[0] = PayloadVersion;
        Buffer.BlockCopy(nonce, 0, payload, 1, NonceLength);
        Buffer.BlockCopy(tag, 0, payload, 1 + NonceLength, TagLength);
        Buffer.BlockCopy(ciphertext, 0, payload, 1 + NonceLength + TagLength, ciphertext.Length);

        CryptographicOperations.ZeroMemory(plaintext);
        return Convert.ToBase64String(payload);
    }

    public string Unprotect(string protectedText)
    {
        if (string.IsNullOrWhiteSpace(protectedText))
            throw new ArgumentException("Protected SMTP password cannot be empty.", nameof(protectedText));

        byte[] payload = Convert.FromBase64String(protectedText);
        int minimumLength = 1 + NonceLength + TagLength + 1;
        if (payload.Length < minimumLength || payload[0] != PayloadVersion)
            throw new CryptographicException("Unsupported or invalid SMTP credential payload.");

        ReadOnlySpan<byte> nonce = payload.AsSpan(1, NonceLength);
        ReadOnlySpan<byte> tag = payload.AsSpan(1 + NonceLength, TagLength);
        ReadOnlySpan<byte> ciphertext = payload.AsSpan(1 + NonceLength + TagLength);
        byte[] plaintext = new byte[ciphertext.Length];

        using AesGcm aes = new(_key, TagLength);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, AssociatedData);

        try
        {
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }
}
