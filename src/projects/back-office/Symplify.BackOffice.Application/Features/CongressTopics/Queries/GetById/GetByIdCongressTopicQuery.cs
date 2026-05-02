using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.CongressTopics.Constants;
using Symplify.BackOffice.Application.Features.CongressTopics.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.CongressTopics.Queries.GetById;
public class GetByIdCongressTopicQuery : IRequest<GetByIdCongressTopicResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { CongressTopicsOperationClaims.Admin, CongressTopicsOperationClaims.Read };
    public class GetByIdCongressTopicQueryHandler : IRequestHandler<GetByIdCongressTopicQuery, GetByIdCongressTopicResponse>
    {
        private readonly ICongressTopicRepository _repository; private readonly IMapper _mapper; private readonly CongressTopicBusinessRules _rules;
        public GetByIdCongressTopicQueryHandler(ICongressTopicRepository repository, IMapper mapper, CongressTopicBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdCongressTopicResponse> Handle(GetByIdCongressTopicQuery request, CancellationToken cancellationToken)
        {
            CongressTopic? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressTopicShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdCongressTopicResponse>(entity);
        }
    }
}
