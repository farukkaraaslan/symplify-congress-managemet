using MediatR;
using Symplify.BackOffice.Application.Services.Repositories;

namespace Symplify.BackOffice.Application.Features.BulkEmails.Commands.TrackOpen;

public sealed class TrackBulkEmailOpenCommand : IRequest<bool>
{
    public Guid TrackingToken { get; set; }

    public sealed class Handler : IRequestHandler<TrackBulkEmailOpenCommand, bool>
    {
        private readonly IMailOutboxMessageRepository _outboxRepository;

        public Handler(IMailOutboxMessageRepository outboxRepository)
        {
            _outboxRepository = outboxRepository;
        }

        public Task<bool> Handle(
            TrackBulkEmailOpenCommand request,
            CancellationToken cancellationToken)
        {
            return _outboxRepository.MarkOpenedAsync(
                request.TrackingToken,
                DateTime.UtcNow,
                cancellationToken);
        }
    }
}
