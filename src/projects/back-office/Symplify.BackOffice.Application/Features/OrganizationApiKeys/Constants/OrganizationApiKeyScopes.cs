namespace Symplify.BackOffice.Application.Features.OrganizationApiKeys.Constants;
public static class OrganizationApiKeyScopes
{
    public const string CongressRead = "Congress.Read";
    public const string SubmissionRead = "Submission.Read";
    public const string SubmissionWrite = "Submission.Write";
    public const string PaymentWrite = "Payment.Write";
    public const string UserRead = "User.Read";
    public const string WebhookSend = "Webhook.Send";
    public static readonly IReadOnlyCollection<string> All = new[] { CongressRead, SubmissionRead, SubmissionWrite, PaymentWrite, UserRead, WebhookSend };
}
