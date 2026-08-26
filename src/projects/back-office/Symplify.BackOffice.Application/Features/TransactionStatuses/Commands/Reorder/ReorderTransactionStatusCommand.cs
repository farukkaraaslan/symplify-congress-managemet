using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.TransactionStatuses.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Commands.Reorder;

public sealed class ReorderTransactionStatusCommand : IRequest<ReorderedTransactionStatusResponse>, ISecuredRequest, ICacheRemoverRequest
{
    /// <summary>
    /// Boş bırakılırsa phase ilk item üzerinden bulunur. Reorder sadece aynı phase içinde yapılır.
    /// </summary>
    public int? TransactionStatusPhaseId { get; set; }

    public ICollection<ReorderTransactionStatusItemDto> Items { get; set; } = new List<ReorderTransactionStatusItemDto>();

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetTransactionStatuses";

    public string[] Roles => new[]
    {
        TransactionStatusesOperationClaims.Admin,
        TransactionStatusesOperationClaims.Write,
        TransactionStatusesOperationClaims.Update
    };

    public sealed class ReorderTransactionStatusCommandHandler
        : IRequestHandler<ReorderTransactionStatusCommand, ReorderedTransactionStatusResponse>
    {
        private readonly ITransactionStatusRepository _repository;

        public ReorderTransactionStatusCommandHandler(ITransactionStatusRepository repository)
        {
            _repository = repository;
        }

        public async Task<ReorderedTransactionStatusResponse> Handle(
            ReorderTransactionStatusCommand request,
            CancellationToken cancellationToken)
        {
            List<ReorderTransactionStatusItemDto> requestedItems = request.Items
                .Where(item => item.Id > 0)
                .GroupBy(item => item.Id)
                .Select(group => group.Last())
                .OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
                .ToList();

            if (requestedItems.Count == 0)
                return new ReorderedTransactionStatusResponse();

            List<TransactionStatus> allVisibleEntities = _repository.Query()
                .ToList()
                .Where(entity => !IsDeleted(entity))
                .ToList();

            Dictionary<int, TransactionStatus> entityById = allVisibleEntities.ToDictionary(entity => entity.Id);

            bool hasMissingEntity = requestedItems.Any(item => !entityById.ContainsKey(item.Id));

            if (hasMissingEntity)
                throw new BusinessException(TransactionStatusesMessages.ReorderEntityNotFound);

            int phaseId = request.TransactionStatusPhaseId
                ?? entityById[requestedItems.First().Id].TransactionStatusPhaseId;

            bool hasDifferentPhase = requestedItems
                .Select(item => entityById[item.Id])
                .Any(entity => entity.TransactionStatusPhaseId != phaseId);

            if (hasDifferentPhase)
                throw new BusinessException(TransactionStatusesMessages.ReorderDifferentPhaseNotAllowed);

            List<TransactionStatus> phaseEntities = allVisibleEntities
                .Where(entity => entity.TransactionStatusPhaseId == phaseId)
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            HashSet<int> requestedIds = requestedItems.Select(item => item.Id).ToHashSet();
            List<TransactionStatus> reorderedEntities = requestedItems.Select(item => entityById[item.Id]).ToList();
            List<TransactionStatus> remainingEntities = phaseEntities
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

            return new ReorderedTransactionStatusResponse
            {
                UpdatedCount = updatedCount
            };
        }

        private async Task<int> PersistNormalizedOrdersAsync(
            IReadOnlyList<TransactionStatus> entities,
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

public sealed class ReorderTransactionStatusItemDto
{
    public int Id { get; set; }

    public int Order { get; set; }
}
