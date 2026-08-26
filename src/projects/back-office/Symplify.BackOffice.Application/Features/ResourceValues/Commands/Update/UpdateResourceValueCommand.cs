using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.ResourceValues.Constants;
using Symplify.BackOffice.Application.Features.ResourceValues.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.ResourceValues.Commands.Update;
public class UpdateResourceValueCommand : IRequest<UpdatedResourceValueResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid ResourceKeyId { get; set; }
    public Guid LanguageId { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetResourceValues";
    public string[] Roles => new[] { ResourceValuesOperationClaims.Admin, ResourceValuesOperationClaims.Write, ResourceValuesOperationClaims.Update };
    public class UpdateResourceValueCommandHandler : IRequestHandler<UpdateResourceValueCommand, UpdatedResourceValueResponse>
    {
        private readonly IResourceValueRepository _repository; private readonly IMapper _mapper; private readonly ResourceValueBusinessRules _rules;
        public UpdateResourceValueCommandHandler(IResourceValueRepository repository, IMapper mapper, ResourceValueBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<UpdatedResourceValueResponse> Handle(UpdateResourceValueCommand request, CancellationToken cancellationToken)
        {
            ResourceValue? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.ResourceValueShouldExistWhenSelected(entity);
            entity!.ResourceKeyId = request.ResourceKeyId;
            entity!.LanguageId = request.LanguageId;
            entity!.Value = request.Value;
            ResourceValue updatedEntity = await _repository.UpdateAsync(entity!);
            return _mapper.Map<UpdatedResourceValueResponse>(updatedEntity);
        }
    }
}
