using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.Topics.Constants;
using Symplify.BackOffice.Application.Features.Topics.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Lookups;

namespace Symplify.BackOffice.Application.Features.Topics.Commands.Delete;

public class DeleteTopicCommand : IRequest<DeletedTopicResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetTopics";

    public string[] Roles => new[] { TopicsOperationClaims.Admin, TopicsOperationClaims.Write, TopicsOperationClaims.Delete };

    public class DeleteTopicCommandHandler : IRequestHandler<DeleteTopicCommand, DeletedTopicResponse>
    {
        private readonly ITopicRepository _repository;
        private readonly IMapper _mapper;
        private readonly TopicBusinessRules _rules;

        public DeleteTopicCommandHandler(
            ITopicRepository repository,
            IMapper mapper,
            TopicBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedTopicResponse> Handle(DeleteTopicCommand request, CancellationToken cancellationToken)
        {
            Topic? entity = await _repository.GetAsync(predicate: item => item.Id.Equals(request.Id));
            await _rules.TopicShouldExistWhenSelected(entity);

            Topic deletedEntity = await _repository.DeleteAsync(entity!);
            await NormalizeVisibleOrdersAsync(request.Id, cancellationToken);

            return _mapper.Map<DeletedTopicResponse>(deletedEntity);
        }

        private async Task NormalizeVisibleOrdersAsync(Guid deletedEntityId, CancellationToken cancellationToken)
        {
            List<Topic> entities = _repository.Query()
                .ToList()
                .Where(entity => !IsDeleted(entity) && entity.Id != deletedEntityId)
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            for (int index = 0; index < entities.Count; index++)
            {
                int normalizedOrder = index + 1;

                if (entities[index].Order == normalizedOrder)
                    continue;

                entities[index].Order = normalizedOrder;
                await _repository.UpdateAsync(entities[index]);
            }
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return deletedDate is not null;
        }
    }
}
