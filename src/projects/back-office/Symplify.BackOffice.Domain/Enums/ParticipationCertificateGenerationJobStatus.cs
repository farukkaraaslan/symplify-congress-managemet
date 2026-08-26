namespace Symplify.BackOffice.Domain.Enums;

public enum ParticipationCertificateGenerationJobStatus
{
    Pending = 1,
    Preparing = 2,
    Processing = 3,
    Completed = 4,
    CompletedWithErrors = 5,
    Failed = 6,
    CancelRequested = 7,
    Cancelled = 8
}
