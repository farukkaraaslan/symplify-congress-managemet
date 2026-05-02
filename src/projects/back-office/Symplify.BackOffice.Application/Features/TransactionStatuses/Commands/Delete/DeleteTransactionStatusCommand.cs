using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Constants;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Delete;
public class DeleteTransactionStatusCommand : IRequest<DeletedTransactionStatusResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public int Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetTransactionStatuses";
    public string[] Roles => new[] { TransactionStatusesOperationClaims.Admin, TransactionStatusesOperationClaims.Write, TransactionStatusesOperationClaims.Delete };
    public class DeleteTransactionStatusCommandHandler : IRequestHandler<DeleteTransactionStatusCommand, DeletedTransactionStatusResponse>
    {
        private readonly ITransactionStatusRepository _repository; private readonly IMapper _mapper; private readonly TransactionStatusBusinessRules _rules;
        public DeleteTransactionStatusCommandHandler(ITransactionStatusRepository repository, IMapper mapper, TransactionStatusBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedTransactionStatusResponse> Handle(DeleteTransactionStatusCommand request, CancellationToken cancellationToken)
        {
            TransactionStatus? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.TransactionStatusShouldExistWhenSelected(entity);
            TransactionStatus deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedTransactionStatusResponse>(deletedEntity);
        }
    }
}
