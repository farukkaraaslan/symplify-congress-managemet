using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressSliders.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressSliders.Commands.Reorder;

public sealed class ReorderCongressSliderCommand : IRequest<ReorderedCongressSliderResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }
    public ICollection<ReorderCongressSliderItemDto> Items { get; set; } = new List<ReorderCongressSliderItemDto>();
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressSliders";
    public string[] Roles => new[] { CongressSlidersOperationClaims.Admin, CongressSlidersOperationClaims.Write, CongressSlidersOperationClaims.Update };

    public sealed class ReorderCongressSliderCommandHandler : IRequestHandler<ReorderCongressSliderCommand, ReorderedCongressSliderResponse>
    {
        private readonly ICongressSliderRepository _repository;

        public ReorderCongressSliderCommandHandler(ICongressSliderRepository repository)
        {
            _repository = repository;
        }

        public async Task<ReorderedCongressSliderResponse> Handle(ReorderCongressSliderCommand request, CancellationToken cancellationToken)
        {
            if (request.CongressId == Guid.Empty)
                throw new BusinessException(CongressSlidersMessages.CongressRequired);

            List<ReorderCongressSliderItemDto> requestedItems = request.Items
                .Where(item => item.Id != Guid.Empty)
                .GroupBy(item => item.Id)
                .Select(group => group.Last())
                .OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
                .ToList();

            if (requestedItems.Count == 0)
                return new ReorderedCongressSliderResponse();

            List<CongressSlider> allVisibleEntities = _repository.Query()
                .ToList()
                .Where(entity => entity.CongressId == request.CongressId && !IsDeleted(entity))
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            Dictionary<Guid, CongressSlider> entityById = allVisibleEntities.ToDictionary(entity => entity.Id);

            if (requestedItems.Any(item => !entityById.ContainsKey(item.Id)))
                throw new BusinessException(CongressSlidersMessages.InvalidReorderList);

            HashSet<Guid> requestedIds = requestedItems.Select(item => item.Id).ToHashSet();
            List<CongressSlider> reorderedEntities = requestedItems.Select(item => entityById[item.Id]).ToList();
            List<CongressSlider> remainingEntities = allVisibleEntities.Where(entity => !requestedIds.Contains(entity.Id)).ToList();

            int insertOrder = requestedItems
                .Where(item => item.Order > 0)
                .Select(item => item.Order)
                .DefaultIfEmpty(1)
                .Min();

            int insertIndex = Math.Clamp(insertOrder - 1, 0, remainingEntities.Count);
            remainingEntities.InsertRange(insertIndex, reorderedEntities);

            int updatedCount = await PersistNormalizedOrdersAsync(remainingEntities, cancellationToken);

            return new ReorderedCongressSliderResponse { UpdatedCount = updatedCount };
        }

        private async Task<int> PersistNormalizedOrdersAsync(IReadOnlyList<CongressSlider> entities, CancellationToken cancellationToken)
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

public sealed class ReorderCongressSliderItemDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
}

public sealed class ReorderedCongressSliderResponse
{
    public int UpdatedCount { get; set; }
}
