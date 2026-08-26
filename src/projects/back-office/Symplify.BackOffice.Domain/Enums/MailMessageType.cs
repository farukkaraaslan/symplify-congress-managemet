namespace Symplify.BackOffice.Domain.Enums;

/// <summary>
/// Business purpose of an outgoing email. Values are persisted; do not renumber existing members.
/// </summary>
public enum MailMessageType
{
    Unknown = 0,

    EmailConfirmation = 10,
    PasswordReset = 20,
    OrganizationMailTest = 30,

    SubmissionSentToReview = 100,
    SubmissionPaymentPending = 110,
    SubmissionPaymentApproved = 120,
    SubmissionAccepted = 130,

    AcceptanceLetter = 200,
    ParticipationCertificate = 210,

    BulkEmail = 300,

    OtherSystem = 900
}
