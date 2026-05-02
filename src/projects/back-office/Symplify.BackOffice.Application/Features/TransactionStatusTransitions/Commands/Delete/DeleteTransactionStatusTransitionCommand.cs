using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Constants;
using Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;
namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Commands.Delete;
public class DeleteTransactionStatusTransitionCommand : IRequest<DeletedTransactionStatusTransitionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public int Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetTransactionStatusTransitions";
    public string[] Roles => new[] { TransactionStatusTransitionsOperationClaims.Admin, TransactionStatusTransitionsOperationClaims.Write, TransactionStatusTransitionsOperationClaims.Delete };
    public class DeleteTransactionStatusTransitionCommandHandler : IRequestHandler<DeleteTransactionStatusTransitionCommand, DeletedTransactionStatusTransitionResponse>
    {
        private readonly ITransactionStatusTransitionRepository _repository; private readonly IMapper _mapper; private readonly TransactionStatusTransitionBusinessRules _rules;
        public DeleteTransactionStatusTransitionCommandHandler(ITransactionStatusTransitionRepository repository, IMapper mapper, TransactionStatusTransitionBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedTransactionStatusTransitionResponse> Handle(DeleteTransactionStatusTransitionCommand request, CancellationToken cancellationToken)
        {
            TransactionStatusTransition? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.TransactionStatusTransitionShouldExistWhenSelected(entity);
            TransactionStatusTransition deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedTransactionStatusTransitionResponse>(deletedEntity);
        }
    }
}
