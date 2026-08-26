using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.PaymentStatuses.Constants;
using Symplify.BackOffice.Application.Features.PaymentStatuses.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.PaymentStatuses.Commands.Delete;
public class DeletePaymentStatusCommand : IRequest<DeletedPaymentStatusResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public int Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetPaymentStatuses";
    public string[] Roles => new[] { PaymentStatusesOperationClaims.Admin, PaymentStatusesOperationClaims.Write, PaymentStatusesOperationClaims.Delete };
    public class DeletePaymentStatusCommandHandler : IRequestHandler<DeletePaymentStatusCommand, DeletedPaymentStatusResponse>
    {
        private readonly IPaymentStatusRepository _repository; private readonly IMapper _mapper; private readonly PaymentStatusBusinessRules _rules;
        public DeletePaymentStatusCommandHandler(IPaymentStatusRepository repository, IMapper mapper, PaymentStatusBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedPaymentStatusResponse> Handle(DeletePaymentStatusCommand request, CancellationToken cancellationToken)
        {
            PaymentStatus? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.PaymentStatusShouldExistWhenSelected(entity);
            PaymentStatus deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedPaymentStatusResponse>(deletedEntity);
        }
    }
}
