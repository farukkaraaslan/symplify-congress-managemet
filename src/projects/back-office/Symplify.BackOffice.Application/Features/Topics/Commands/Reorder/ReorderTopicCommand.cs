using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.Topics.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.Topics.Commands.Reorder;

public sealed class ReorderTopicCommand : IRequest<ReorderedTopicResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public ICollection<ReorderTopicItemDto> Items { get; set; } = new List<ReorderTopicItemDto>();

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetTopics";

    public string[] Roles => new[] { TopicsOperationClaims.Admin, TopicsOperationClaims.Write, TopicsOperationClaims.Update };

    public sealed class ReorderTopicCommandHandler : IRequestHandler<ReorderTopicCommand, ReorderedTopicResponse>
    {
        private readonly ITopicRepository _repository;

        public ReorderTopicCommandHandler(ITopicRepository repository)
        {
            _repository = repository;
        }

        public async Task<ReorderedTopicResponse> Handle(ReorderTopicCommand request, CancellationToken cancellationToken)
        {
            List<ReorderTopicItemDto> requestedItems = request.Items
                .Where(item => item.Id != Guid.Empty)
                .GroupBy(item => item.Id)
                .Select(group => group.Last())
                .OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
                .ToList();

            if (requestedItems.Count == 0)
                return new ReorderedTopicResponse();

            List<Topic> allVisibleEntities = _repository.Query()
                .ToList()
                .Where(entity => !IsDeleted(entity))
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            Dictionary<Guid, Topic> entityById = allVisibleEntities.ToDictionary(entity => entity.Id);

            bool hasMissingEntity = requestedItems.Any(item => !entityById.ContainsKey(item.Id));

            if (hasMissingEntity)
                throw new BusinessException("Sıralama güncellenecek kayıt bulunamadı.");

            HashSet<Guid> requestedIds = requestedItems.Select(item => item.Id).ToHashSet();
            List<Topic> reorderedEntities = requestedItems.Select(item => entityById[item.Id]).ToList();
            List<Topic> remainingEntities = allVisibleEntities.Where(entity => !requestedIds.Contains(entity.Id)).ToList();

            int insertOrder = requestedItems
                .Where(item => item.Order > 0)
                .Select(item => item.Order)
                .DefaultIfEmpty(1)
                .Min();

            int insertIndex = Math.Clamp(insertOrder - 1, 0, remainingEntities.Count);
            remainingEntities.InsertRange(insertIndex, reorderedEntities);

            int updatedCount = await PersistNormalizedOrdersAsync(remainingEntities, cancellationToken);

            return new ReorderedTopicResponse
            {
                UpdatedCount = updatedCount
            };
        }

        private async Task<int> PersistNormalizedOrdersAsync(
            IReadOnlyList<Topic> entities,
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

public sealed class ReorderTopicItemDto
{
    public Guid Id { get; set; }

    public int Order { get; set; }
}
