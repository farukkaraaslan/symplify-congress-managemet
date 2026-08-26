namespace Symplify.BackOffice.Application.Services.Email;

/// <summary>
/// Low-level transport abstraction. Application features should normally use IApplicationMailQueueService
/// so every outgoing email is persisted before transport.
/// </summary>
public interface IBackOfficeEmailSender
{
    Task<BackOfficeEmailSendResult> SendAsync(
        BackOfficeEmailMessage message,
        CancellationToken cancellationToken = default);
}
