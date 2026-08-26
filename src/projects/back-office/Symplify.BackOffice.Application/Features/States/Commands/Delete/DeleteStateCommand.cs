using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.States.Constants;
using Symplify.BackOffice.Application.Features.States.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
namespace Symplify.BackOffice.Application.Features.States.Commands.Delete;
public class DeleteStateCommand : IRequest<DeletedStateResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetStates";
    public string[] Roles => new[] { StatesOperationClaims.Admin, StatesOperationClaims.Write, StatesOperationClaims.Delete };
    public class DeleteStateCommandHandler : IRequestHandler<DeleteStateCommand, DeletedStateResponse>
    {
        private readonly IStateRepository _repository; private readonly IMapper _mapper; private readonly StateBusinessRules _rules;
        public DeleteStateCommandHandler(IStateRepository repository, IMapper mapper, StateBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedStateResponse> Handle(DeleteStateCommand request, CancellationToken cancellationToken)
        {
            State? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.StateShouldExistWhenSelected(entity);
            State deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedStateResponse>(deletedEntity);
        }
    }
}
