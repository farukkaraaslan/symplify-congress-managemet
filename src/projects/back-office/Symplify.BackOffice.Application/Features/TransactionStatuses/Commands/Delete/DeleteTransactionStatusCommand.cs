using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
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

    public string[] Roles => new[]
    {
        TransactionStatusesOperationClaims.Admin,
        TransactionStatusesOperationClaims.Write,
        TransactionStatusesOperationClaims.Delete
    };

    public class DeleteTransactionStatusCommandHandler : IRequestHandler<DeleteTransactionStatusCommand, DeletedTransactionStatusResponse>
    {
        private readonly ITransactionStatusRepository _repository;
        private readonly IMapper _mapper;
        private readonly TransactionStatusBusinessRules _rules;

        public DeleteTransactionStatusCommandHandler(
            ITransactionStatusRepository repository,
            IMapper mapper,
            TransactionStatusBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedTransactionStatusResponse> Handle(
            DeleteTransactionStatusCommand request,
            CancellationToken cancellationToken)
        {
            TransactionStatus? entity = await _repository.GetAsync(predicate: root => root.Id.Equals(request.Id));
            await _rules.TransactionStatusShouldExistWhenSelected(entity);
            await _rules.TransactionStatusShouldNotBeUsedByTransition(request.Id);
            await _rules.TransactionStatusShouldNotBeUsedBySubmission(request.Id);
            await _rules.TransactionStatusShouldNotBeUsedBySubmissionHistory(request.Id);

            int phaseId = entity!.TransactionStatusPhaseId;

            TransactionStatus deletedEntity = await _repository.DeleteAsync(entity);

            await NormalizePhaseOrdersAfterDeleteAsync(phaseId, cancellationToken);

            return _mapper.Map<DeletedTransactionStatusResponse>(deletedEntity);
        }

        private async Task NormalizePhaseOrdersAfterDeleteAsync(int phaseId, CancellationToken cancellationToken)
        {
            List<TransactionStatus> entities = _repository.Query()
                .ToList()
                .Where(entity => !IsDeleted(entity) && entity.TransactionStatusPhaseId == phaseId)
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
