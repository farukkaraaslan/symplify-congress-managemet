using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Constants;
using Symplify.BackOffice.Application.Features.CongressAnnouncements.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressAnnouncements.Commands.Reorder;

public class ReorderCongressAnnouncementsCommand : IRequest<ReorderedCongressAnnouncementsResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }

    public ICollection<ReorderCongressAnnouncementItemDto> Items { get; set; } =
        new List<ReorderCongressAnnouncementItemDto>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressAnnouncements";
    public string[] Roles => new[] { CongressAnnouncementsOperationClaims.Admin, CongressAnnouncementsOperationClaims.Write, CongressAnnouncementsOperationClaims.Update };

    public class Handler : IRequestHandler<ReorderCongressAnnouncementsCommand, ReorderedCongressAnnouncementsResponse>
    {
        private readonly ICongressAnnouncementRepository _repository;
        private readonly CongressAnnouncementBusinessRules _rules;

        public Handler(
            ICongressAnnouncementRepository repository,
            CongressAnnouncementBusinessRules rules)
        {
            _repository = repository;
            _rules = rules;
        }

        public async Task<ReorderedCongressAnnouncementsResponse> Handle(
            ReorderCongressAnnouncementsCommand request,
            CancellationToken cancellationToken)
        {
            await _rules.CongressShouldExist(request.CongressId, cancellationToken);

            List<ReorderCongressAnnouncementItemDto> requestedItems = request.Items
                .Where(item => item.Id != Guid.Empty)
                .GroupBy(item => item.Id)
                .Select(group => group.Last())
                .OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
                .ToList();

            await _rules.ReorderItemsShouldBeValid(requestedItems);

            List<CongressAnnouncement> allVisibleEntities = _repository.Query()
                .ToList()
                .Where(entity =>
                    entity.CongressId == request.CongressId &&
                    !IsDeleted(entity))
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            Dictionary<Guid, CongressAnnouncement> entityById = allVisibleEntities
                .ToDictionary(entity => entity.Id);

            await _rules.ReorderItemsShouldBelongToCongress(requestedItems, entityById);

            HashSet<Guid> requestedIds = requestedItems
                .Select(item => item.Id)
                .ToHashSet();

            List<CongressAnnouncement> reorderedEntities = requestedItems
                .Select(item => entityById[item.Id])
                .ToList();

            List<CongressAnnouncement> remainingEntities = allVisibleEntities
                .Where(entity => !requestedIds.Contains(entity.Id))
                .ToList();

            int insertOrder = requestedItems
                .Where(item => item.Order > 0)
                .Select(item => item.Order)
                .DefaultIfEmpty(1)
                .Min();

            int insertIndex = Math.Clamp(insertOrder - 1, 0, remainingEntities.Count);

            remainingEntities.InsertRange(insertIndex, reorderedEntities);

            int updatedCount = await PersistNormalizedOrdersAsync(
                remainingEntities,
                cancellationToken);

            return new ReorderedCongressAnnouncementsResponse
            {
                CongressId = request.CongressId,
                UpdatedCount = updatedCount,
                OrderedIds = remainingEntities.Select(entity => entity.Id).ToList()
            };
        }

        private async Task<int> PersistNormalizedOrdersAsync(
            IReadOnlyList<CongressAnnouncement> entities,
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

public sealed class ReorderCongressAnnouncementItemDto
{
    public Guid Id { get; set; }

    public int Order { get; set; }
}
