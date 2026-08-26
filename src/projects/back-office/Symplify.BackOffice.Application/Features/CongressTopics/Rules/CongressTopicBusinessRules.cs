using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressTopics.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.CongressTopics.Rules;

public class CongressTopicBusinessRules : BaseBusinessRules
{
    private readonly ICongressRepository _congressRepository;
    private readonly ITopicRepository _topicRepository;

    public CongressTopicBusinessRules(
        ICongressRepository congressRepository,
        ITopicRepository topicRepository)
    {
        _congressRepository = congressRepository;
        _topicRepository = topicRepository;
    }

    public async Task<Congress> CongressShouldExist(Guid congressId, CancellationToken cancellationToken)
    {
        if (congressId == Guid.Empty)
            throw new BusinessException(CongressTopicsMessages.CongressRequired);

        Congress? congress = await _congressRepository.GetAsync(
            predicate: entity => entity.Id == congressId,
            cancellationToken: cancellationToken);

        if (congress is null)
            throw new BusinessException(CongressTopicsMessages.CongressNotFound);

        return congress;
    }

    public Task CongressTopicShouldExistWhenSelected(CongressTopic? entity)
    {
        if (entity is null)
            throw new BusinessException(CongressTopicsMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public Task SelectionListShouldBeValid(IReadOnlyCollection<Guid> topicIds)
    {
        if (topicIds.Any(id => id == Guid.Empty))
            throw new BusinessException(CongressTopicsMessages.InvalidSelectionList);

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Topic>> TopicsShouldExist(
        IReadOnlyCollection<Guid> topicIds,
        CancellationToken cancellationToken)
    {
        if (topicIds.Count == 0)
            return Array.Empty<Topic>();

        List<Topic> topics = _topicRepository
            .Query()
            .ToList()
            .Where(topic => topicIds.Contains(topic.Id) && !IsDeleted(topic))
            .ToList();

        HashSet<Guid> existingIds = topics.Select(topic => topic.Id).ToHashSet();

        if (topicIds.Any(id => !existingIds.Contains(id)))
            throw new BusinessException(CongressTopicsMessages.TopicNotFound);

        return await Task.FromResult(topics);
    }

    private static bool IsDeleted(object entity)
    {
        object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
        return deletedDate is not null;
    }
}
