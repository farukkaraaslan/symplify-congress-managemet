using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.ResourceKeys.Constants;
using Symplify.BackOffice.Application.Features.ResourceKeys.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.ResourceKeys.Commands.Delete;
public class DeleteResourceKeyCommand : IRequest<DeletedResourceKeyResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetResourceKeys";
    public string[] Roles => new[] { ResourceKeysOperationClaims.Admin, ResourceKeysOperationClaims.Write, ResourceKeysOperationClaims.Delete };
    public class DeleteResourceKeyCommandHandler : IRequestHandler<DeleteResourceKeyCommand, DeletedResourceKeyResponse>
    {
        private readonly IResourceKeyRepository _repository; private readonly IMapper _mapper; private readonly ResourceKeyBusinessRules _rules;
        public DeleteResourceKeyCommandHandler(IResourceKeyRepository repository, IMapper mapper, ResourceKeyBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedResourceKeyResponse> Handle(DeleteResourceKeyCommand request, CancellationToken cancellationToken)
        {
            ResourceKey? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.ResourceKeyShouldExistWhenSelected(entity);
            ResourceKey deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedResourceKeyResponse>(deletedEntity);
        }
    }
}
