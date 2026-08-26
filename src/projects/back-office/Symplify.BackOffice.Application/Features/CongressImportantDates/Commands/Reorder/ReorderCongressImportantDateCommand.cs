using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Constants;
using Symplify.BackOffice.Application.Features.CongressImportantDates.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressImportantDates.Commands.Reorder;

public sealed class ReorderCongressImportantDateCommand
    : IRequest<ReorderedCongressImportantDateResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }

    public ICollection<ReorderCongressImportantDateItemDto> Items { get; set; }
        = new List<ReorderCongressImportantDateItemDto>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressImportantDates";

    public string[] Roles => new[]
    {
        CongressImportantDatesOperationClaims.Admin,
        CongressImportantDatesOperationClaims.Write,
        CongressImportantDatesOperationClaims.Update
    };

    public sealed class ReorderCongressImportantDateCommandHandler
        : IRequestHandler<ReorderCongressImportantDateCommand, ReorderedCongressImportantDateResponse>
    {
        private readonly ICongressImportantDateRepository _repository;
        private readonly CongressImportantDateBusinessRules _rules;

        public ReorderCongressImportantDateCommandHandler(
            ICongressImportantDateRepository repository,
            CongressImportantDateBusinessRules rules)
        {
            _repository = repository;
            _rules = rules;
        }

        public async Task<ReorderedCongressImportantDateResponse> Handle(
            ReorderCongressImportantDateCommand request,
            CancellationToken cancellationToken)
        {
            await _rules.CongressShouldExist(request.CongressId, cancellationToken);

            List<ReorderCongressImportantDateItemDto> requestedItems = request.Items
                .Where(item => item.Id != Guid.Empty)
                .GroupBy(item => item.Id)
                .Select(group => group.Last())
                .OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
                .ToList();

            IReadOnlyCollection<Guid> requestedIdsForRule = requestedItems.Select(item => item.Id).ToList();
            await _rules.ReorderItemsShouldBeValid(requestedIdsForRule);

            List<CongressImportantDate> allVisibleEntities = _repository.Query()
                .ToList()
                .Where(entity => entity.CongressId == request.CongressId && !IsDeleted(entity))
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.StartDate)
                .ThenBy(entity => entity.EndDate)
                .ThenBy(entity => entity.Id)
                .ToList();

            Dictionary<Guid, CongressImportantDate> entityById = allVisibleEntities.ToDictionary(entity => entity.Id);

            await _rules.ReorderItemsShouldBelongToCongress(requestedIdsForRule, entityById);

            HashSet<Guid> requestedIds = requestedItems.Select(item => item.Id).ToHashSet();
            List<CongressImportantDate> reorderedEntities = requestedItems.Select(item => entityById[item.Id]).ToList();
            List<CongressImportantDate> remainingEntities = allVisibleEntities.Where(entity => !requestedIds.Contains(entity.Id)).ToList();

            int insertOrder = requestedItems
                .Where(item => item.Order > 0)
                .Select(item => item.Order)
                .DefaultIfEmpty(1)
                .Min();

            int insertIndex = Math.Clamp(insertOrder - 1, 0, remainingEntities.Count);
            remainingEntities.InsertRange(insertIndex, reorderedEntities);

            int updatedCount = await PersistNormalizedOrdersAsync(remainingEntities, cancellationToken);

            return new ReorderedCongressImportantDateResponse
            {
                UpdatedCount = updatedCount
            };
        }

        private async Task<int> PersistNormalizedOrdersAsync(
            IReadOnlyList<CongressImportantDate> entities,
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

public sealed class ReorderCongressImportantDateItemDto
{
    public Guid Id { get; set; }

    public int Order { get; set; }
}
