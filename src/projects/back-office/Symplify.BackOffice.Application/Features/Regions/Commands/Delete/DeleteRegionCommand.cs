using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.Regions.Constants;
using Symplify.BackOffice.Application.Features.Regions.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Reference;
namespace Symplify.BackOffice.Application.Features.Regions.Commands.Delete;
public class DeleteRegionCommand : IRequest<DeletedRegionResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetRegions";
    public string[] Roles => new[] { RegionsOperationClaims.Admin, RegionsOperationClaims.Write, RegionsOperationClaims.Delete };
    public class DeleteRegionCommandHandler : IRequestHandler<DeleteRegionCommand, DeletedRegionResponse>
    {
        private readonly IRegionRepository _repository; private readonly IMapper _mapper; private readonly RegionBusinessRules _rules;
        public DeleteRegionCommandHandler(IRegionRepository repository, IMapper mapper, RegionBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<DeletedRegionResponse> Handle(DeleteRegionCommand request, CancellationToken cancellationToken)
        {
            Region? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.RegionShouldExistWhenSelected(entity);
            Region deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedRegionResponse>(deletedEntity);
        }
    }
}
