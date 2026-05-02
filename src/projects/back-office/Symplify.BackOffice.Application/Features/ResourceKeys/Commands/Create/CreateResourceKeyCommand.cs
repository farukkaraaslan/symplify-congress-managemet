using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.ResourceKeys.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.ResourceKeys.Commands.Create;
public class CreateResourceKeyCommand : IRequest<CreatedResourceKeyResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public string KeyName { get; set; } = string.Empty;
    public string? AreaName { get; set; }
    public string? Description { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetResourceKeys";
    public string[] Roles => new[] { ResourceKeysOperationClaims.Admin, ResourceKeysOperationClaims.Write, ResourceKeysOperationClaims.Add };
    public class CreateResourceKeyCommandHandler : IRequestHandler<CreateResourceKeyCommand, CreatedResourceKeyResponse>
    {
        private readonly IResourceKeyRepository _repository;
        private readonly IMapper _mapper;
        public CreateResourceKeyCommandHandler(IResourceKeyRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedResourceKeyResponse> Handle(CreateResourceKeyCommand request, CancellationToken cancellationToken)
        {
            ResourceKey entity = new()
            {
                Id = Guid.NewGuid(),
                KeyName = request.KeyName,
                AreaName = request.AreaName,
                Description = request.Description,
            };
            ResourceKey createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedResourceKeyResponse>(createdEntity);
        }
    }
}
