using Core.Application.Pipelines.Authorization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.MailDeliveries.Constants;
using Symplify.BackOffice.Application.Features.MailDeliveries.Dtos;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Communication;

namespace Symplify.BackOffice.Application.Features.MailDeliveries.Queries.GetDetail;

public sealed class GetMailDeliveryDetailQuery : IRequest<GetMailDeliveryDetailResponse?>, ISecuredRequest
{
    public Guid Id { get; set; }
    public Guid? CurrentUserId { get; set; }
    public bool IsSuperAdmin { get; set; }

    public string[] Roles =>
    [
        MailDeliveriesOperationClaims.Admin,
        MailDeliveriesOperationClaims.Read
    ];

    public sealed class Handler : IRequestHandler<GetMailDeliveryDetailQuery, GetMailDeliveryDetailResponse?>
    {
        private readonly IMailOutboxMessageRepository _outboxRepository;
        private readonly IMailDeliveryEventRepository _eventRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationUserRepository _organizationUserRepository;
        private readonly ICongressRepository _congressRepository;
        private readonly ISubmissionRepository _submissionRepository;
        private readonly ISubmissionAcceptanceLetterRepository _acceptanceLetterRepository;
        private readonly IParticipationCertificateRepository _participationCertificateRepository;

        public Handler(
            IMailOutboxMessageRepository outboxRepository,
            IMailDeliveryEventRepository eventRepository,
            IOrganizationRepository organizationRepository,
            IOrganizationUserRepository organizationUserRepository,
            ICongressRepository congressRepository,
            ISubmissionRepository submissionRepository,
            ISubmissionAcceptanceLetterRepository acceptanceLetterRepository,
            IParticipationCertificateRepository participationCertificateRepository)
        {
            _outboxRepository = outboxRepository;
            _eventRepository = eventRepository;
            _organizationRepository = organizationRepository;
            _organizationUserRepository = organizationUserRepository;
            _congressRepository = congressRepository;
            _submissionRepository = submissionRepository;
            _acceptanceLetterRepository = acceptanceLetterRepository;
            _participationCertificateRepository = participationCertificateRepository;
        }

        public async Task<GetMailDeliveryDetailResponse?> Handle(
            GetMailDeliveryDetailQuery request,
            CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
                return null;

            IQueryable<MailOutboxMessage> query = _outboxRepository.Query().AsNoTracking();

            if (!request.IsSuperAdmin)
            {
                if (!request.CurrentUserId.HasValue || request.CurrentUserId.Value == Guid.Empty)
                    return null;

                Guid userId = request.CurrentUserId.Value;
                IQueryable<Guid> allowedOrganizations = _organizationUserRepository.Query()
                    .AsNoTracking()
                    .Where(item => item.UserId == userId && item.IsActive && item.DeletedDate == null)
                    .Select(item => item.OrganizationId);

                query = query.Where(item =>
                    item.OrganizationId.HasValue &&
                    allowedOrganizations.Contains(item.OrganizationId.Value));
            }

            MailOutboxMessage? message = await query.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
            if (message is null)
                return null;

            string organizationName = string.Empty;
            if (message.OrganizationId.HasValue)
            {
                organizationName = await _organizationRepository.Query()
                    .AsNoTracking()
                    .Where(item => item.Id == message.OrganizationId.Value)
                    .Select(item => item.Name)
                    .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
            }

            string congressName = string.Empty;
            if (message.CongressId.HasValue)
            {
                congressName = await _congressRepository.Query()
                    .AsNoTracking()
                    .Where(item => item.Id == message.CongressId.Value)
                    .Select(item => item.Name)
                    .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
            }

            string? submissionNumber = null;
            if (message.RelatedSubmissionId.HasValue)
            {
                submissionNumber = await _submissionRepository.Query()
                    .AsNoTracking()
                    .Where(item => item.Id == message.RelatedSubmissionId.Value)
                    .Select(item => item.SubmissionNumber)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            string? acceptanceLetterNumber = null;
            if (message.AcceptanceLetterId.HasValue)
            {
                acceptanceLetterNumber = await _acceptanceLetterRepository.Query()
                    .AsNoTracking()
                    .Where(item => item.Id == message.AcceptanceLetterId.Value)
                    .Select(item => item.LetterNumber)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            string? certificateFileName = null;
            if (message.ParticipationCertificateId.HasValue)
            {
                certificateFileName = await _participationCertificateRepository.Query()
                    .AsNoTracking()
                    .Where(item => item.Id == message.ParticipationCertificateId.Value)
                    .Select(item => item.FileName)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            List<MailDeliveryEventDto> events = await _eventRepository.Query()
                .AsNoTracking()
                .Where(item => item.MailOutboxMessageId == message.Id)
                .OrderBy(item => item.OccurredAt)
                .ThenBy(item => item.CreatedDate)
                .Select(item => new MailDeliveryEventDto
                {
                    Id = item.Id,
                    EventType = item.EventType,
                    OccurredAt = item.OccurredAt,
                    ProviderMessageId = item.ProviderMessageId,
                    StatusCode = item.StatusCode,
                    DiagnosticCode = item.DiagnosticCode,
                    BounceType = item.BounceType,
                    BounceSubType = item.BounceSubType,
                    SmtpResponse = item.SmtpResponse,
                    Detail = item.Detail
                })
                .ToListAsync(cancellationToken);

            return new GetMailDeliveryDetailResponse
            {
                Id = message.Id,
                MailType = message.MailType,
                RecipientName = message.ToName ?? string.Empty,
                RecipientEmail = message.ToEmail,
                Subject = message.Subject,
                OrganizationId = message.OrganizationId,
                OrganizationName = organizationName,
                CongressId = message.CongressId,
                CongressName = congressName,
                RelatedUserId = message.RelatedUserId,
                RelatedAuthorId = message.RelatedAuthorId,
                RelatedSubmissionId = message.RelatedSubmissionId,
                SubmissionNumber = submissionNumber,
                AcceptanceLetterId = message.AcceptanceLetterId,
                AcceptanceLetterNumber = acceptanceLetterNumber,
                ParticipationCertificateId = message.ParticipationCertificateId,
                ParticipationCertificateFileName = certificateFileName,
                BulkEmailBatchId = message.BulkEmailBatchId,
                Status = message.Status,
                DeliveryStatus = message.DeliveryStatus,
                AttemptCount = message.AttemptCount,
                CreatedAt = message.CreatedDate,
                LastAttemptAt = message.LastAttemptAt,
                SentAt = message.SentAt,
                DeliveredAt = message.DeliveredAt,
                BouncedAt = message.BouncedAt,
                ComplainedAt = message.ComplainedAt,
                LastDeliveryEventAt = message.LastDeliveryEventAt,
                Provider = message.Provider,
                ProviderMessageId = message.ProviderMessageId,
                LastError = message.LastError,
                DeliveryStatusCode = message.DeliveryStatusCode,
                DeliveryDiagnosticCode = message.DeliveryDiagnosticCode,
                DeliverySmtpResponse = message.DeliverySmtpResponse,
                BounceType = message.BounceType,
                BounceSubType = message.BounceSubType,
                Events = events
            };
        }
    }
}
