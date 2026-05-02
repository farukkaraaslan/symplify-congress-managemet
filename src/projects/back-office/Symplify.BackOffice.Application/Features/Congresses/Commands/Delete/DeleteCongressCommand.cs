using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Congresses.Constants;
using Symplify.BackOffice.Application.Features.Congresses.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
namespace Symplify.BackOffice.Application.Features.Congresses.Commands.Delete;
public class DeleteCongressCommand : IRequest<DeletedCongressResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongresses";
    public string[] Roles => new[] { CongressesOperationClaims.Admin, CongressesOperationClaims.Write, CongressesOperationClaims.Delete };
    public class DeleteCongressCommandHandler : IRequestHandler<DeleteCongressCommand, DeletedCongressResponse>
    {
        private readonly ICongressRepository _repository; private readonly IMapper _mapper; private readonly CongressBusinessRules _rules;
        public DeleteCongressCommandHandler(ICongressRepository repository, IMapper mapper, CongressBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedCongressResponse> Handle(DeleteCongressCommand request, CancellationToken cancellationToken)
        {
            Congress? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressShouldExistWhenSelected(entity);
            Congress deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedCongressResponse>(deletedEntity);
        }
    }
}
