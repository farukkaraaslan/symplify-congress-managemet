using System.Text.Json.Serialization;

namespace Symplify.BackOffice.Infrastructure.Email.Ses;

public sealed class AmazonSnsEnvelope
{
    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("MessageId")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("TopicArn")]
    public string TopicArn { get; set; } = string.Empty;

    [JsonPropertyName("Subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("Message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("Timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("SignatureVersion")]
    public string SignatureVersion { get; set; } = string.Empty;

    [JsonPropertyName("Signature")]
    public string Signature { get; set; } = string.Empty;

    [JsonPropertyName("SigningCertURL")]
    public string SigningCertUrl { get; set; } = string.Empty;

    [JsonPropertyName("SubscribeURL")]
    public string? SubscribeUrl { get; set; }

    [JsonPropertyName("Token")]
    public string? Token { get; set; }
}
