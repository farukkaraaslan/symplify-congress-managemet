using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatusPhases.Constants;
using Symplify.BackOffice.Application.Features.TransactionStatusPhases.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Commands.Delete;

public class DeleteTransactionStatusPhaseCommand
    : IRequest<DeletedTransactionStatusPhaseResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public int Id { get; set; }

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetTransactionStatusPhases";

    public string[] Roles => new[]
    {
        TransactionStatusPhasesOperationClaims.Admin,
        TransactionStatusPhasesOperationClaims.Write,
        TransactionStatusPhasesOperationClaims.Delete
    };

    public class DeleteTransactionStatusPhaseCommandHandler
        : IRequestHandler<DeleteTransactionStatusPhaseCommand, DeletedTransactionStatusPhaseResponse>
    {
        private readonly ITransactionStatusPhaseRepository _repository;
        private readonly IMapper _mapper;
        private readonly TransactionStatusPhaseBusinessRules _rules;

        public DeleteTransactionStatusPhaseCommandHandler(
            ITransactionStatusPhaseRepository repository,
            IMapper mapper,
            TransactionStatusPhaseBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedTransactionStatusPhaseResponse> Handle(
            DeleteTransactionStatusPhaseCommand request,
            CancellationToken cancellationToken)
        {
            TransactionStatusPhase? entity = await _repository.GetAsync(predicate: root => root.Id.Equals(request.Id));
            await _rules.TransactionStatusPhaseShouldExistWhenSelected(entity);
            await _rules.TransactionStatusPhaseShouldNotHaveTransactionStatuses(request.Id);

            int deletedOrder = entity!.Order;

            TransactionStatusPhase deletedEntity = await _repository.DeleteAsync(entity);

            await NormalizeOrdersAfterDeleteAsync(deletedOrder, cancellationToken);

            return _mapper.Map<DeletedTransactionStatusPhaseResponse>(deletedEntity);
        }

        private async Task NormalizeOrdersAfterDeleteAsync(int deletedOrder, CancellationToken cancellationToken)
        {
            List<TransactionStatusPhase> entities = _repository.Query()
                .ToList()
                .Where(entity => !IsDeleted(entity))
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            for (int index = 0; index < entities.Count; index++)
            {
                int normalizedOrder = index + 1;

                if (entities[index].Order == normalizedOrder)
                    continue;

                entities[index].Order = normalizedOrder;
                await _repository.UpdateAsync(entities[index]);
            }
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}
