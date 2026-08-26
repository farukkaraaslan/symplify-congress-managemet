using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatusPhases.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Commands.Reorder;

public sealed class ReorderTransactionStatusPhaseCommand
    : IRequest<ReorderedTransactionStatusPhaseResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public ICollection<ReorderTransactionStatusPhaseItemDto> Items { get; set; } = new List<ReorderTransactionStatusPhaseItemDto>();

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetTransactionStatusPhases";

    public string[] Roles => new[]
    {
        TransactionStatusPhasesOperationClaims.Admin,
        TransactionStatusPhasesOperationClaims.Write,
        TransactionStatusPhasesOperationClaims.Update
    };

    public sealed class ReorderTransactionStatusPhaseCommandHandler
        : IRequestHandler<ReorderTransactionStatusPhaseCommand, ReorderedTransactionStatusPhaseResponse>
    {
        private readonly ITransactionStatusPhaseRepository _repository;

        public ReorderTransactionStatusPhaseCommandHandler(ITransactionStatusPhaseRepository repository)
        {
            _repository = repository;
        }

        public async Task<ReorderedTransactionStatusPhaseResponse> Handle(
            ReorderTransactionStatusPhaseCommand request,
            CancellationToken cancellationToken)
        {
            List<ReorderTransactionStatusPhaseItemDto> requestedItems = request.Items
                .Where(item => item.Id > 0)
                .GroupBy(item => item.Id)
                .Select(group => group.Last())
                .OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
                .ToList();

            if (requestedItems.Count == 0)
                return new ReorderedTransactionStatusPhaseResponse();

            List<TransactionStatusPhase> allVisibleEntities = _repository.Query()
                .ToList()
                .Where(entity => !IsDeleted(entity))
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            Dictionary<int, TransactionStatusPhase> entityById = allVisibleEntities.ToDictionary(entity => entity.Id);

            bool hasMissingEntity = requestedItems.Any(item => !entityById.ContainsKey(item.Id));

            if (hasMissingEntity)
                throw new BusinessException(TransactionStatusPhasesMessages.ReorderEntityNotFound);

            HashSet<int> requestedIds = requestedItems.Select(item => item.Id).ToHashSet();
            List<TransactionStatusPhase> reorderedEntities = requestedItems.Select(item => entityById[item.Id]).ToList();
            List<TransactionStatusPhase> remainingEntities = allVisibleEntities
                .Where(entity => !requestedIds.Contains(entity.Id))
                .ToList();

            int insertOrder = requestedItems
                .Where(item => item.Order > 0)
                .Select(item => item.Order)
                .DefaultIfEmpty(1)
                .Min();

            int insertIndex = Math.Clamp(insertOrder - 1, 0, remainingEntities.Count);
            remainingEntities.InsertRange(insertIndex, reorderedEntities);

            int updatedCount = await PersistNormalizedOrdersAsync(remainingEntities, cancellationToken);

            return new ReorderedTransactionStatusPhaseResponse
            {
                UpdatedCount = updatedCount
            };
        }

        private async Task<int> PersistNormalizedOrdersAsync(
            IReadOnlyList<TransactionStatusPhase> entities,
            CancellationToken cancellationToken)
        {
            int updatedCount = 0;

            for (int index = 0; index < entities.Count; index++)
            {
                int normalizedOrder = index + 1;

                if (entities[index].Order == normalizedOrder)
                    continue;

                entities[index].Order = normalizedOrder;
                await _repository.UpdateAsync(entities[index]);
                updatedCount++;
            }

            return updatedCount;
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}

public sealed class ReorderTransactionStatusPhaseItemDto
{
    public int Id { get; set; }

    public int Order { get; set; }
}
