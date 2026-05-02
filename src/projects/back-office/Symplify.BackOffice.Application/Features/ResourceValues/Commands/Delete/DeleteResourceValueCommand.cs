using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.ResourceValues.Constants;
using Symplify.BackOffice.Application.Features.ResourceValues.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.ResourceValues.Commands.Delete;
public class DeleteResourceValueCommand : IRequest<DeletedResourceValueResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetResourceValues";
    public string[] Roles => new[] { ResourceValuesOperationClaims.Admin, ResourceValuesOperationClaims.Write, ResourceValuesOperationClaims.Delete };
    public class DeleteResourceValueCommandHandler : IRequestHandler<DeleteResourceValueCommand, DeletedResourceValueResponse>
    {
        private readonly IResourceValueRepository _repository; private readonly IMapper _mapper; private readonly ResourceValueBusinessRules _rules;
        public DeleteResourceValueCommandHandler(IResourceValueRepository repository, IMapper mapper, ResourceValueBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedResourceValueResponse> Handle(DeleteResourceValueCommand request, CancellationToken cancellationToken)
        {
            ResourceValue? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.ResourceValueShouldExistWhenSelected(entity);
            ResourceValue deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedResourceValueResponse>(deletedEntity);
        }
    }
}
