using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Constants;
using Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Rules;
using Symplify.BackOffice.Application.Features.Organizations.Constants;
using Symplify.BackOffice.Application.Services.Email;
using Symplify.BackOffice.Application.Services.Mailing;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;
using Symplify.BackOffice.Domain.Communication;
using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Application.Features.OrganizationMailConfigurations.Commands.SendTest;

public sealed class SendOrganizationMailTestCommand : IRequest<SendOrganizationMailTestResponse>, ISecuredRequest
{
    public Guid OrganizationId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string? ToName { get; set; }

    public string[] Roles =>
    [
        OrganizationsOperationClaims.Admin,
        OrganizationsOperationClaims.Write,
        OrganizationsOperationClaims.Update
    ];

    public sealed class Handler : IRequestHandler<SendOrganizationMailTestCommand, SendOrganizationMailTestResponse>
    {
        private readonly OrganizationMailConfigurationBusinessRules _rules;
        private readonly IOrganizationMailConfigurationRepository _repository;
        private readonly IMailOutboxMessageRepository _outboxRepository;
        private readonly IApplicationMailQueueService _mailQueueService;
        private readonly IBackOfficeEmailSender _emailSender;

        public Handler(
            OrganizationMailConfigurationBusinessRules rules,
            IOrganizationMailConfigurationRepository repository,
            IMailOutboxMessageRepository outboxRepository,
            IApplicationMailQueueService mailQueueService,
            IBackOfficeEmailSender emailSender)
        {
            _rules = rules;
            _repository = repository;
            _outboxRepository = outboxRepository;
            _mailQueueService = mailQueueService;
            _emailSender = emailSender;
        }

        public async Task<SendOrganizationMailTestResponse> Handle(
            SendOrganizationMailTestCommand request,
            CancellationToken cancellationToken)
        {
            OrganizationMailConfiguration entity = await _rules.ConfigurationShouldExistAsync(
                request.OrganizationId,
                cancellationToken);

            DateTime testedAt = DateTime.UtcNow;

            MailOutboxMessage outbox = await _mailQueueService.QueueAsync(
                new MailQueueRequest
                {
                    Message = new BackOfficeEmailMessage
                    {
                        OrganizationId = request.OrganizationId,
                        ToEmail = request.ToEmail.Trim(),
                        ToName = Normalize(request.ToName),
                        Subject = "Symplify organizasyon mail ayarları test mesajı",
                        HtmlBody = BuildTestHtml(entity)
                    },
                    MailType = MailMessageType.OrganizationMailTest,
                    ImmediateDispatch = true,
                    CreatedBy = "OrganizationMailConfigurationTest"
                },
                cancellationToken);

            outbox.AttemptCount = 1;
            outbox.LastAttemptAt = testedAt;

            BackOfficeEmailSendResult sendResult;
            try
            {
                sendResult = await _emailSender.SendAsync(
                    new BackOfficeEmailMessage
                    {
                        TrackingId = outbox.Id,
                        MailType = outbox.MailType,
                        OrganizationId = request.OrganizationId,
                        ToEmail = request.ToEmail.Trim(),
                        ToName = Normalize(request.ToName),
                        Subject = outbox.Subject,
                        HtmlBody = outbox.HtmlBody
                    },
                    cancellationToken);
            }
            catch (Exception exception)
            {
                outbox.Status = MailOutboxStatus.Failed;
                outbox.LastError = Truncate(exception.Message, 1000);
                outbox.UpdatedDate = DateTime.UtcNow;
                outbox.UpdatedBy = "OrganizationMailConfigurationTest";
                await _outboxRepository.UpdateAsync(outbox);

                entity.LastTestedAt = testedAt;
                entity.LastTestSucceeded = false;
                entity.LastTestError = Truncate(exception.Message, 1000);
                entity.UpdatedDate = testedAt;
                entity.UpdatedBy = "OrganizationMailConfigurationTest";
                await _repository.UpdateAsync(entity);

                throw new BusinessException(OrganizationMailConfigurationsMessages.TestMailFailed);
            }

            MailDeliveryStatus initialDeliveryStatus = sendResult.DeliveryTrackingEnabled
                ? MailDeliveryStatus.Pending
                : MailDeliveryStatus.NotTracked;

            bool markedAsSent = await _outboxRepository.MarkTransportSentAsync(
                outbox.Id,
                testedAt,
                outbox.AttemptCount,
                outbox.LastAttemptAt ?? testedAt,
                sendResult.Provider,
                initialDeliveryStatus,
                redactedHtmlBody: null,
                updatedBy: "OrganizationMailConfigurationTest",
                cancellationToken);

            if (!markedAsSent)
                throw new InvalidOperationException($"Mail outbox message {outbox.Id} could not be marked as sent.");

            entity.LastTestedAt = testedAt;
            entity.LastTestSucceeded = true;
            entity.LastTestError = null;
            entity.UpdatedDate = testedAt;
            entity.UpdatedBy = "OrganizationMailConfigurationTest";
            await _repository.UpdateAsync(entity);

            return new SendOrganizationMailTestResponse
            {
                OrganizationId = request.OrganizationId,
                SentAt = testedAt
            };
        }

        private static string BuildTestHtml(OrganizationMailConfiguration entity)
        {
            string safeFromName = System.Net.WebUtility.HtmlEncode(entity.FromName);
            string logoHtml = !string.IsNullOrWhiteSpace(entity.MailLogoBucketName) &&
                              !string.IsNullOrWhiteSpace(entity.MailLogoObjectName)
                ? $"""
                  <img src="cid:{MailBrandingModel.OrganizationLogoContentId}"
                       width="160"
                       alt="{safeFromName}"
                       style="display:block;max-width:160px;height:auto;margin:0 auto 20px;" />
                  """
                : $"""
                  <h2 style="text-align:center;color:#0f285f;">{safeFromName}</h2>
                  """;

            return $"""
                <div style="font-family:Arial,sans-serif;max-width:640px;margin:auto;padding:24px;">
                    {logoHtml}
                    <p>
                        Bu e-posta, organizasyona bağlı SMTP ayarlarının ve private MinIO mail logosunun
                        doğru çalıştığını doğrulamak için gönderilmiştir.
                    </p>
                </div>
                """;
        }

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string Truncate(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];
    }
}
