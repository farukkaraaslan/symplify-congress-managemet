using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressTopics.Constants;
using Symplify.BackOffice.Application.Features.CongressTopics.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressTopics.Commands.Update;
public class UpdateCongressTopicCommand : IRequest<UpdatedCongressTopicResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public Guid TopicId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressTopics";
    public string[] Roles => new[] { CongressTopicsOperationClaims.Admin, CongressTopicsOperationClaims.Write, CongressTopicsOperationClaims.Update };
    public class UpdateCongressTopicCommandHandler : IRequestHandler<UpdateCongressTopicCommand, UpdatedCongressTopicResponse>
    {
        private readonly ICongressTopicRepository _repository; private readonly IMapper _mapper; private readonly CongressTopicBusinessRules _rules;
        public UpdateCongressTopicCommandHandler(ICongressTopicRepository repository, IMapper mapper, CongressTopicBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedCongressTopicResponse> Handle(UpdateCongressTopicCommand request, CancellationToken cancellationToken)
        {
            CongressTopic? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressTopicShouldExistWhenSelected(entity);
            entity!.CongressId = request.CongressId;
            entity!.TopicId = request.TopicId;
            entity!.Order = request.Order;
            entity!.IsActive = request.IsActive;
            CongressTopic updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedCongressTopicResponse>(updatedEntity);
        }
    }
}
