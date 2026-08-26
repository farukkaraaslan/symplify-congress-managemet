using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressTopics.Constants;
using Symplify.BackOffice.Application.Features.CongressTopics.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.CongressTopics.Commands.SyncSelections;

public sealed class SyncCongressTopicSelectionsCommand
    : IRequest<SyncedCongressTopicSelectionsResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }
    public ICollection<Guid> SelectedTopicIds { get; set; } = new List<Guid>();
    public ICollection<CongressTopicSelectionAssignmentDto> Assignments { get; set; } = new List<CongressTopicSelectionAssignmentDto>();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressTopics";

    public string[] Roles => new[]
    {
        CongressTopicsOperationClaims.Admin,
        CongressTopicsOperationClaims.Write,
        CongressTopicsOperationClaims.Update
    };

    public sealed class SyncCongressTopicSelectionsCommandHandler
        : IRequestHandler<SyncCongressTopicSelectionsCommand, SyncedCongressTopicSelectionsResponse>
    {
        private readonly ICongressTopicRepository _repository;
        private readonly CongressTopicBusinessRules _rules;
        private readonly ICongressTopicCategoryRepository _categoryRepository;

        public SyncCongressTopicSelectionsCommandHandler(
            ICongressTopicRepository repository,
            CongressTopicBusinessRules rules,
            ICongressTopicCategoryRepository categoryRepository)
        {
            _repository = repository;
            _rules = rules;
            _categoryRepository = categoryRepository;
        }

        public async Task<SyncedCongressTopicSelectionsResponse> Handle(
            SyncCongressTopicSelectionsCommand request,
            CancellationToken cancellationToken)
        {
            await _rules.CongressShouldExist(request.CongressId, cancellationToken);

            bool hasAssignments = request.Assignments.Count > 0;

            List<Guid> selectedIds = (hasAssignments
                    ? request.Assignments.Select(item => item.TopicId)
                    : request.SelectedTopicIds)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            await _rules.SelectionListShouldBeValid(selectedIds);
            IReadOnlyList<Topic> selectedTopics = await _rules.TopicsShouldExist(selectedIds, cancellationToken);

            Dictionary<Guid, Guid?> categoryByTopicId = request.Assignments
                .Where(item => item.TopicId != Guid.Empty)
                .GroupBy(item => item.TopicId)
                .ToDictionary(group => group.Key, group => group.Last().CategoryId);

            HashSet<Guid> categoryIds = categoryByTopicId.Values
                .Where(id => id.HasValue && id.Value != Guid.Empty)
                .Select(id => id!.Value)
                .ToHashSet();

            if (categoryIds.Count > 0)
            {
                HashSet<Guid> validCategoryIds = _categoryRepository
                    .Query()
                    .ToList()
                    .Where(item => item.CongressId == request.CongressId &&
                                   categoryIds.Contains(item.Id) &&
                                   !IsDeleted(item))
                    .Select(item => item.Id)
                    .ToHashSet();

                if (!categoryIds.SetEquals(validCategoryIds))
                    throw new BusinessException("BackOffice.CongressTopics.Validation.CategoryNotFound");
            }

            List<CongressTopic> existingRelations = _repository
                .Query()
                .ToList()
                .Where(entity => entity.CongressId == request.CongressId && !IsDeleted(entity))
                .ToList();

            HashSet<Guid> selectedIdSet = selectedIds.ToHashSet();
            Dictionary<Guid, CongressTopic> existingByTopicId = existingRelations
                .GroupBy(entity => entity.TopicId)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(entity => entity.Id).First());

            int deletedCount = 0;
            int addedCount = 0;
            int updatedCount = 0;

            foreach (CongressTopic relation in existingRelations.Where(entity => !selectedIdSet.Contains(entity.TopicId)))
            {
                await _repository.DeleteAsync(relation);
                deletedCount++;
            }

            List<Topic> orderedSelectedTopics = selectedTopics
                .OrderBy(topic => topic.Order <= 0 ? int.MaxValue : topic.Order)
                .ThenBy(topic => topic.Id)
                .ToList();

            for (int index = 0; index < orderedSelectedTopics.Count; index++)
            {
                Topic topic = orderedSelectedTopics[index];
                int normalizedOrder = index + 1;

                if (existingByTopicId.TryGetValue(topic.Id, out CongressTopic? relation))
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

                    if (hasAssignments)
                    {
                        Guid? requestedCategoryId = categoryByTopicId.GetValueOrDefault(topic.Id);
                        if (relation.CategoryId != requestedCategoryId)
                        {
                            relation.CategoryId = requestedCategoryId;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        await _repository.UpdateAsync(relation);
                        updatedCount++;
                    }

                    continue;
                }

                CongressTopic newRelation = new()
                {
                    Id = Guid.NewGuid(),
                    CongressId = request.CongressId,
                    TopicId = topic.Id,
                    CategoryId = hasAssignments ? categoryByTopicId.GetValueOrDefault(topic.Id) : null,
                    Order = normalizedOrder,
                    IsActive = true
                };

                await _repository.AddAsync(newRelation);
                addedCount++;
            }

            return new SyncedCongressTopicSelectionsResponse
            {
                AddedCount = addedCount,
                UpdatedCount = updatedCount,
                DeletedCount = deletedCount,
                SelectedCount = orderedSelectedTopics.Count
            };
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}
