using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.SubmissionTypes.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.SubmissionTypes.Commands.Reorder;

public sealed class ReorderSubmissionTypeCommand : IRequest<ReorderedSubmissionTypeResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public ICollection<ReorderSubmissionTypeItemDto> Items { get; set; } = new List<ReorderSubmissionTypeItemDto>();

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetSubmissionTypes";

    public string[] Roles => new[] { SubmissionTypesOperationClaims.Admin, SubmissionTypesOperationClaims.Write, SubmissionTypesOperationClaims.Update };

    public sealed class ReorderSubmissionTypeCommandHandler : IRequestHandler<ReorderSubmissionTypeCommand, ReorderedSubmissionTypeResponse>
    {
        private readonly ISubmissionTypeRepository _repository;

        public ReorderSubmissionTypeCommandHandler(ISubmissionTypeRepository repository)
        {
            _repository = repository;
        }

        public async Task<ReorderedSubmissionTypeResponse> Handle(ReorderSubmissionTypeCommand request, CancellationToken cancellationToken)
        {
            List<ReorderSubmissionTypeItemDto> requestedItems = request.Items
                .Where(item => item.Id != Guid.Empty)
                .GroupBy(item => item.Id)
                .Select(group => group.Last())
                .OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
                .ToList();

            if (requestedItems.Count == 0)
                return new ReorderedSubmissionTypeResponse();

            List<SubmissionType> allVisibleEntities = _repository.Query()
                .ToList()
                .Where(entity => !IsDeleted(entity))
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            Dictionary<Guid, SubmissionType> entityById = allVisibleEntities.ToDictionary(entity => entity.Id);

            bool hasMissingEntity = requestedItems.Any(item => !entityById.ContainsKey(item.Id));

            if (hasMissingEntity)
                throw new BusinessException("Sıralama güncellenecek kayıt bulunamadı.");

            HashSet<Guid> requestedIds = requestedItems.Select(item => item.Id).ToHashSet();
            List<SubmissionType> reorderedEntities = requestedItems.Select(item => entityById[item.Id]).ToList();
            List<SubmissionType> remainingEntities = allVisibleEntities.Where(entity => !requestedIds.Contains(entity.Id)).ToList();

            int insertOrder = requestedItems
                .Where(item => item.Order > 0)
                .Select(item => item.Order)
                .DefaultIfEmpty(1)
                .Min();

            int insertIndex = Math.Clamp(insertOrder - 1, 0, remainingEntities.Count);
            remainingEntities.InsertRange(insertIndex, reorderedEntities);

            int updatedCount = await PersistNormalizedOrdersAsync(remainingEntities, cancellationToken);

            return new ReorderedSubmissionTypeResponse
            {
                UpdatedCount = updatedCount
            };
        }

        private async Task<int> PersistNormalizedOrdersAsync(
            IReadOnlyList<SubmissionType> entities,
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

public sealed class ReorderSubmissionTypeItemDto
{
    public Guid Id { get; set; }

    public int Order { get; set; }
}
