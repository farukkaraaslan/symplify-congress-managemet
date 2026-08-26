using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressTopics.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressTopics.Commands.Create;
public class CreateCongressTopicCommand : IRequest<CreatedCongressTopicResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid CongressId { get; set; }
    public Guid TopicId { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressTopics";
    public string[] Roles => new[] { CongressTopicsOperationClaims.Admin, CongressTopicsOperationClaims.Write, CongressTopicsOperationClaims.Add };
    public class CreateCongressTopicCommandHandler : IRequestHandler<CreateCongressTopicCommand, CreatedCongressTopicResponse>
    {
        private readonly ICongressTopicRepository _repository;
        private readonly IMapper _mapper;
        public CreateCongressTopicCommandHandler(ICongressTopicRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedCongressTopicResponse> Handle(CreateCongressTopicCommand request, CancellationToken cancellationToken)
        {
            CongressTopic entity = new()
            {
                Id = Guid.NewGuid(),
                CongressId = request.CongressId,
                TopicId = request.TopicId,
                Order = request.Order,
                IsActive = request.IsActive,
            };
            CongressTopic createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedCongressTopicResponse>(createdEntity);
        }
    }
}
