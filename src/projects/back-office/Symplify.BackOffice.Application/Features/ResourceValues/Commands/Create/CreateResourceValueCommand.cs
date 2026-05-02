using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.ResourceValues.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.ResourceValues.Commands.Create;
public class CreateResourceValueCommand : IRequest<CreatedResourceValueResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid ResourceKeyId { get; set; }
    public Guid LanguageId { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetResourceValues";
    public string[] Roles => new[] { ResourceValuesOperationClaims.Admin, ResourceValuesOperationClaims.Write, ResourceValuesOperationClaims.Add };
    public class CreateResourceValueCommandHandler : IRequestHandler<CreateResourceValueCommand, CreatedResourceValueResponse>
    {
        private readonly IResourceValueRepository _repository;
        private readonly IMapper _mapper;
        public CreateResourceValueCommandHandler(IResourceValueRepository repository, IMapper mapper) { _repository = repository; _mapper = mapper; }
        public async Task<CreatedResourceValueResponse> Handle(CreateResourceValueCommand request, CancellationToken cancellationToken)
        {
            ResourceValue entity = new()
            {
                Id = Guid.NewGuid(),
                ResourceKeyId = request.ResourceKeyId,
                LanguageId = request.LanguageId,
                Value = request.Value,
            };
            ResourceValue createdEntity = await _repository.AddAsync(entity);
            return _mapper.Map<CreatedResourceValueResponse>(createdEntity);
        }
    }
}
