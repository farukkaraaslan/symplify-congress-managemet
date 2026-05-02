using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.ResourceKeys.Constants;
using Symplify.BackOffice.Application.Features.ResourceKeys.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.ResourceKeys.Commands.Update;
public class UpdateResourceKeyCommand : IRequest<UpdatedResourceKeyResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public string KeyName { get; set; } = string.Empty;
    public string? AreaName { get; set; }
    public string? Description { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetResourceKeys";
    public string[] Roles => new[] { ResourceKeysOperationClaims.Admin, ResourceKeysOperationClaims.Write, ResourceKeysOperationClaims.Update };
    public class UpdateResourceKeyCommandHandler : IRequestHandler<UpdateResourceKeyCommand, UpdatedResourceKeyResponse>
    {
        private readonly IResourceKeyRepository _repository; private readonly IMapper _mapper; private readonly ResourceKeyBusinessRules _rules;
        public UpdateResourceKeyCommandHandler(IResourceKeyRepository repository, IMapper mapper, ResourceKeyBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedResourceKeyResponse> Handle(UpdateResourceKeyCommand request, CancellationToken cancellationToken)
        {
            ResourceKey? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.ResourceKeyShouldExistWhenSelected(entity);
            entity!.KeyName = request.KeyName;
            entity!.AreaName = request.AreaName;
            entity!.Description = request.Description;
            ResourceKey updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedResourceKeyResponse>(updatedEntity);
        }
    }
}
