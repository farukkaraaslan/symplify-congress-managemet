using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Constants;
using Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.CongressSubmissionTypes.Commands.SyncSelections;

public sealed class SyncCongressSubmissionTypeSelectionsCommand
    : IRequest<SyncedCongressSubmissionTypeSelectionsResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }
    public ICollection<Guid> SelectedSubmissionTypeIds { get; set; } = new List<Guid>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressSubmissionTypes";

    public string[] Roles => new[]
    {
        CongressSubmissionTypesOperationClaims.Admin,
        CongressSubmissionTypesOperationClaims.Write,
        CongressSubmissionTypesOperationClaims.Update
    };

    public sealed class SyncCongressSubmissionTypeSelectionsCommandHandler
        : IRequestHandler<SyncCongressSubmissionTypeSelectionsCommand, SyncedCongressSubmissionTypeSelectionsResponse>
    {
        private readonly ICongressSubmissionTypeRepository _repository;
        private readonly CongressSubmissionTypeBusinessRules _rules;

        public SyncCongressSubmissionTypeSelectionsCommandHandler(
            ICongressSubmissionTypeRepository repository,
            CongressSubmissionTypeBusinessRules rules)
        {
            _repository = repository;
            _rules = rules;
        }

        public async Task<SyncedCongressSubmissionTypeSelectionsResponse> Handle(
            SyncCongressSubmissionTypeSelectionsCommand request,
            CancellationToken cancellationToken)
        {
            await _rules.CongressShouldExist(request.CongressId, cancellationToken);

            List<Guid> selectedIds = request.SelectedSubmissionTypeIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            await _rules.SelectionListShouldBeValid(selectedIds);
            IReadOnlyList<SubmissionType> selectedSubmissionTypes = await _rules.SubmissionTypesShouldExist(selectedIds, cancellationToken);

            List<CongressSubmissionType> existingRelations = _repository
                .Query()
                .ToList()
                .Where(entity => entity.CongressId == request.CongressId && !IsDeleted(entity))
                .ToList();

            HashSet<Guid> selectedIdSet = selectedIds.ToHashSet();
            Dictionary<Guid, CongressSubmissionType> existingBySubmissionTypeId = existingRelations
                .GroupBy(entity => entity.SubmissionTypeId)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(entity => entity.Id).First());

            int deletedCount = 0;
            int addedCount = 0;
            int updatedCount = 0;

            foreach (CongressSubmissionType relation in existingRelations.Where(entity => !selectedIdSet.Contains(entity.SubmissionTypeId)))
            {
                await _repository.DeleteAsync(relation);
                deletedCount++;
            }

            List<SubmissionType> orderedSelectedSubmissionTypes = selectedSubmissionTypes
                .OrderBy(submissionType => submissionType.Order <= 0 ? int.MaxValue : submissionType.Order)
                .ThenBy(submissionType => submissionType.Id)
                .ToList();

            for (int index = 0; index < orderedSelectedSubmissionTypes.Count; index++)
            {
                SubmissionType submissionType = orderedSelectedSubmissionTypes[index];
                int normalizedOrder = index + 1;

                if (existingBySubmissionTypeId.TryGetValue(submissionType.Id, out CongressSubmissionType? relation))
                {
                    bool changed = false;

                    if (relation.Order != normalizedOrder)
                    {
                        relation.Order = normalizedOrder;
                        changed = true;
                    }

                    if (!relation.IsActive)
                    {
                        relation.IsActive = true;
                        changed = true;
                    }

                    if (changed)
                    {
                        await _repository.UpdateAsync(relation);
                        updatedCount++;
                    }

                    continue;
                }

                CongressSubmissionType newRelation = new()
                {
                    Id = Guid.NewGuid(),
                    CongressId = request.CongressId,
                    SubmissionTypeId = submissionType.Id,
                    Order = normalizedOrder,
                    IsActive = true
                };

                await _repository.AddAsync(newRelation);
                addedCount++;
            }

            return new SyncedCongressSubmissionTypeSelectionsResponse
            {
                AddedCount = addedCount,
                UpdatedCount = updatedCount,
                DeletedCount = deletedCount,
                SelectedCount = orderedSelectedSubmissionTypes.Count
            };
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}
