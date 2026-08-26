using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Features.MailDeliveries.Dtos;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Infrastructure.Email.Ses;

public sealed class AmazonSesSnsAdapter : IAmazonSesSnsAdapter
{
    private const int MaxPayloadLength = 512 * 1024;
    private static readonly ConcurrentDictionary<string, byte[]> CertificateCache = new(StringComparer.Ordinal);
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(15) };

    private readonly BackOfficeMailOptions _options;
    private readonly ILogger<AmazonSesSnsAdapter> _logger;

    public AmazonSesSnsAdapter(
        IOptions<BackOfficeMailOptions> options,
        ILogger<AmazonSesSnsAdapter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AmazonSnsEnvelope> ParseAndValidateAsync(
        string rawJson,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawJson) || rawJson.Length > MaxPayloadLength)
            throw new InvalidOperationException("Invalid Amazon SNS payload.");

        AmazonSnsEnvelope envelope = JsonSerializer.Deserialize<AmazonSnsEnvelope>(rawJson)
            ?? throw new InvalidOperationException("Amazon SNS payload could not be parsed.");

        ValidateRequiredEnvelopeFields(envelope);
        ValidateTopicArn(envelope.TopicArn);

        if (_options.SesTracking.VerifySnsSignature)
        {
            bool signatureValid = await VerifySignatureAsync(envelope, cancellationToken);
            if (!signatureValid)
                throw new InvalidOperationException("Amazon SNS signature validation failed.");
        }

        return envelope;
    }

    public async Task ConfirmSubscriptionAsync(
        AmazonSnsEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        if (!_options.SesTracking.AutoConfirmSubscription ||
            !string.Equals(envelope.Type, "SubscriptionConfirmation", StringComparison.Ordinal))
        {
            return;
        }

        Uri subscribeUri = ValidateAmazonSnsUrl(envelope.SubscribeUrl, "SubscribeURL");
        using HttpResponseMessage response = await HttpClient.GetAsync(subscribeUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Amazon SNS subscription confirmed for topic {TopicArn}.", envelope.TopicArn);
    }

    public MailDeliveryProviderEventDto? ParseSesEvent(AmazonSnsEnvelope envelope)
    {
        if (!string.Equals(envelope.Type, "Notification", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(envelope.Message))
        {
            return null;
        }

        using JsonDocument document = JsonDocument.Parse(envelope.Message);
        JsonElement root = document.RootElement;

        string? eventTypeText = GetString(root, "eventType") ?? GetString(root, "notificationType");
        MailDeliveryEventType eventType = ParseEventType(eventTypeText);
        if (eventType == MailDeliveryEventType.Unknown)
            return null;

        if (!root.TryGetProperty("mail", out JsonElement mail))
            return null;

        Guid? outboxId = ParseOutboxId(mail);
        if (!outboxId.HasValue || outboxId.Value == Guid.Empty)
            return null;

        string? providerMessageId = GetString(mail, "messageId");
        string? destinationEmail = GetFirstStringFromArray(mail, "destination");
        DateTime occurredAt = ParseTimestamp(GetString(mail, "timestamp")) ?? DateTime.UtcNow;

        MailDeliveryProviderEventDto result = new()
        {
            MailOutboxMessageId = outboxId.Value,
            ProviderEventId = envelope.MessageId,
            ProviderMessageId = providerMessageId,
            EventType = eventType,
            OccurredAt = occurredAt,
            DestinationEmail = destinationEmail
        };

        switch (eventType)
        {
            case MailDeliveryEventType.Delivery:
                if (root.TryGetProperty("delivery", out JsonElement delivery))
                {
                    result.OccurredAt = ParseTimestamp(GetString(delivery, "timestamp")) ?? result.OccurredAt;
                    result.SmtpResponse = GetString(delivery, "smtpResponse");
                }
                break;

            case MailDeliveryEventType.Bounce:
                if (root.TryGetProperty("bounce", out JsonElement bounce))
                {
                    result.OccurredAt = ParseTimestamp(GetString(bounce, "timestamp")) ?? result.OccurredAt;
                    result.BounceType = GetString(bounce, "bounceType");
                    result.BounceSubType = GetString(bounce, "bounceSubType");

                    JsonElement? recipient = FindRecipient(bounce, "bouncedRecipients", destinationEmail);
                    if (recipient.HasValue)
                    {
                        result.StatusCode = GetString(recipient.Value, "status");
                        result.DiagnosticCode = GetString(recipient.Value, "diagnosticCode");
                    }
                }
                break;

            case MailDeliveryEventType.Complaint:
                if (root.TryGetProperty("complaint", out JsonElement complaint))
                {
                    result.OccurredAt = ParseTimestamp(GetString(complaint, "timestamp")) ?? result.OccurredAt;
                    result.Detail = GetString(complaint, "complaintFeedbackType")
                        ?? GetString(complaint, "userAgent");
                }
                break;

            case MailDeliveryEventType.Reject:
                if (root.TryGetProperty("reject", out JsonElement reject))
                    result.Detail = GetString(reject, "reason");
                break;

            case MailDeliveryEventType.DeliveryDelay:
                if (root.TryGetProperty("deliveryDelay", out JsonElement delay))
                {
                    result.OccurredAt = ParseTimestamp(GetString(delay, "timestamp")) ?? result.OccurredAt;
                    result.Detail = GetString(delay, "delayType");

                    JsonElement? recipient = FindRecipient(delay, "delayedRecipients", destinationEmail);
                    if (recipient.HasValue)
                    {
                        result.StatusCode = GetString(recipient.Value, "status");
                        result.DiagnosticCode = GetString(recipient.Value, "diagnosticCode");
                    }
                }
                break;

            case MailDeliveryEventType.RenderingFailure:
                if (root.TryGetProperty("failure", out JsonElement failure))
                {
                    result.OccurredAt = ParseTimestamp(GetString(failure, "timestamp")) ?? result.OccurredAt;
                    result.Detail = GetString(failure, "errorMessage") ?? GetString(failure, "templateName");
                }
                break;
        }

        return result;
    }

    private async Task<bool> VerifySignatureAsync(
        AmazonSnsEnvelope envelope,
        CancellationToken cancellationToken)
    {
        Uri certUri = ValidateAmazonSnsUrl(envelope.SigningCertUrl, "SigningCertURL");
        byte[] certBytes;

        if (!CertificateCache.TryGetValue(certUri.AbsoluteUri, out certBytes!))
        {
            string pem = await HttpClient.GetStringAsync(certUri, cancellationToken);
            certBytes = Encoding.UTF8.GetBytes(pem);
            CertificateCache.TryAdd(certUri.AbsoluteUri, certBytes);
        }

        string pemText = Encoding.UTF8.GetString(certBytes);
        using X509Certificate2 certificate = X509Certificate2.CreateFromPem(pemText);
        using RSA? rsa = certificate.GetRSAPublicKey();
        if (rsa is null)
            return false;

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(envelope.Signature);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] canonicalBytes = Encoding.UTF8.GetBytes(BuildCanonicalString(envelope));
        HashAlgorithmName algorithm = envelope.SignatureVersion switch
        {
            "1" => HashAlgorithmName.SHA1,
            "2" => HashAlgorithmName.SHA256,
            _ => throw new InvalidOperationException("Unsupported Amazon SNS signature version.")
        };

        return rsa.VerifyData(canonicalBytes, signature, algorithm, RSASignaturePadding.Pkcs1);
    }

    private void ValidateTopicArn(string topicArn)
    {
        string[] allowed = _options.SesTracking.AllowedTopicArns
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToArray();

        if (allowed.Length == 0)
            throw new InvalidOperationException("Mail:SesTracking:AllowedTopicArns must contain the SES SNS topic ARN.");

        if (!allowed.Contains(topicArn, StringComparer.Ordinal))
            throw new InvalidOperationException("Amazon SNS topic is not allowed.");
    }

    private static void ValidateRequiredEnvelopeFields(AmazonSnsEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.Type) ||
            string.IsNullOrWhiteSpace(envelope.MessageId) ||
            string.IsNullOrWhiteSpace(envelope.TopicArn) ||
            string.IsNullOrWhiteSpace(envelope.Timestamp) ||
            string.IsNullOrWhiteSpace(envelope.Signature) ||
            string.IsNullOrWhiteSpace(envelope.SignatureVersion) ||
            string.IsNullOrWhiteSpace(envelope.SigningCertUrl))
        {
            throw new InvalidOperationException("Amazon SNS payload is missing required signature fields.");
        }
    }

    private static Uri ValidateAmazonSnsUrl(string? value, string fieldName)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrWhiteSpace(uri.UserInfo) ||
            !IsAmazonSnsHost(uri.Host))
        {
            throw new InvalidOperationException($"Amazon SNS {fieldName} is not trusted.");
        }

        return uri;
    }

    private static bool IsAmazonSnsHost(string host)
    {
        string normalized = host.Trim().TrimEnd('.').ToLowerInvariant();
        return (normalized == "sns.amazonaws.com" ||
                (normalized.StartsWith("sns.") && normalized.EndsWith(".amazonaws.com")) ||
                (normalized.StartsWith("sns.") && normalized.EndsWith(".amazonaws.com.cn")));
    }

    private static string BuildCanonicalString(AmazonSnsEnvelope envelope)
    {
        StringBuilder builder = new();

        Append(builder, "Message", envelope.Message);
        Append(builder, "MessageId", envelope.MessageId);

        if (string.Equals(envelope.Type, "Notification", StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(envelope.Subject))
                Append(builder, "Subject", envelope.Subject);
        }
        else if (string.Equals(envelope.Type, "SubscriptionConfirmation", StringComparison.Ordinal) ||
                 string.Equals(envelope.Type, "UnsubscribeConfirmation", StringComparison.Ordinal))
        {
            Append(builder, "SubscribeURL", envelope.SubscribeUrl ?? string.Empty);
        }

        Append(builder, "Timestamp", envelope.Timestamp);

        if (!string.Equals(envelope.Type, "Notification", StringComparison.Ordinal))
            Append(builder, "Token", envelope.Token ?? string.Empty);

        Append(builder, "TopicArn", envelope.TopicArn);
        Append(builder, "Type", envelope.Type);

        return builder.ToString();
    }

    private static void Append(StringBuilder builder, string key, string value)
    {
        builder.Append(key).Append('\n');
        builder.Append(value).Append('\n');
    }

    private static Guid? ParseOutboxId(JsonElement mail)
    {
        if (!mail.TryGetProperty("tags", out JsonElement tags) || tags.ValueKind != JsonValueKind.Object)
            return null;

        foreach (JsonProperty property in tags.EnumerateObject())
        {
            if (!string.Equals(property.Name, "symplifyOutboxId", StringComparison.OrdinalIgnoreCase))
                continue;

            string? value = property.Value.ValueKind switch
            {
                JsonValueKind.Array => property.Value.EnumerateArray().Select(element => element.GetString()).FirstOrDefault(),
                JsonValueKind.String => property.Value.GetString(),
                _ => null
            };

            if (Guid.TryParseExact(value, "N", out Guid exact) || Guid.TryParse(value, out exact))
                return exact;
        }

        return null;
    }

    private static MailDeliveryEventType ParseEventType(string? value)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (normalized.Equals("Send", StringComparison.OrdinalIgnoreCase))
            return MailDeliveryEventType.Send;
        if (normalized.Equals("Delivery", StringComparison.OrdinalIgnoreCase))
            return MailDeliveryEventType.Delivery;
        if (normalized.Equals("DeliveryDelay", StringComparison.OrdinalIgnoreCase))
            return MailDeliveryEventType.DeliveryDelay;
        if (normalized.Equals("Bounce", StringComparison.OrdinalIgnoreCase))
            return MailDeliveryEventType.Bounce;
        if (normalized.Equals("Reject", StringComparison.OrdinalIgnoreCase))
            return MailDeliveryEventType.Reject;
        if (normalized.Equals("Complaint", StringComparison.OrdinalIgnoreCase))
            return MailDeliveryEventType.Complaint;
        if (normalized.Equals("Rendering Failure", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("RenderingFailure", StringComparison.OrdinalIgnoreCase))
            return MailDeliveryEventType.RenderingFailure;

        return MailDeliveryEventType.Unknown;
    }

    private static JsonElement? FindRecipient(JsonElement container, string propertyName, string? email)
    {
        if (!container.TryGetProperty(propertyName, out JsonElement recipients) ||
            recipients.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        JsonElement? first = null;
        foreach (JsonElement recipient in recipients.EnumerateArray())
        {
            first ??= recipient;
            string? recipientEmail = GetString(recipient, "emailAddress");
            if (!string.IsNullOrWhiteSpace(email) &&
                string.Equals(recipientEmail, email, StringComparison.OrdinalIgnoreCase))
            {
                return recipient;
            }
        }

        return first;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property))
            return null;

        return property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
    }

    private static string? GetFirstStringFromArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (JsonElement item in property.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
                return item.GetString();
        }

        return null;
    }

    private static DateTime? ParseTimestamp(string? value)
    {
        return DateTimeOffset.TryParse(value, out DateTimeOffset parsed)
            ? parsed.UtcDateTime
            : null;
    }
}
