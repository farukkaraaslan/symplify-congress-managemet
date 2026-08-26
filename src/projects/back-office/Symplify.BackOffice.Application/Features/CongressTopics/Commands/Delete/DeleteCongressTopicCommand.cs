using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressTopics.Constants;
using Symplify.BackOffice.Application.Features.CongressTopics.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressTopics.Commands.Delete;
public class DeleteCongressTopicCommand : IRequest<DeletedCongressTopicResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressTopics";
    public string[] Roles => new[] { CongressTopicsOperationClaims.Admin, CongressTopicsOperationClaims.Write, CongressTopicsOperationClaims.Delete };
    public class DeleteCongressTopicCommandHandler : IRequestHandler<DeleteCongressTopicCommand, DeletedCongressTopicResponse>
    {
        private readonly ICongressTopicRepository _repository; private readonly IMapper _mapper; private readonly CongressTopicBusinessRules _rules;
        public DeleteCongressTopicCommandHandler(ICongressTopicRepository repository, IMapper mapper, CongressTopicBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedCongressTopicResponse> Handle(DeleteCongressTopicCommand request, CancellationToken cancellationToken)
        {
            CongressTopic? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressTopicShouldExistWhenSelected(entity);
            CongressTopic deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedCongressTopicResponse>(deletedEntity);
        }
    }
}
