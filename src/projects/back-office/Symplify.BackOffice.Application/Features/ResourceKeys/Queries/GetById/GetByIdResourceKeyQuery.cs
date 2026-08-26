using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.ResourceKeys.Constants;
using Symplify.BackOffice.Application.Features.ResourceKeys.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Localization;
namespace Symplify.BackOffice.Application.Features.ResourceKeys.Queries.GetById;
public class GetByIdResourceKeyQuery : IRequest<GetByIdResourceKeyResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { ResourceKeysOperationClaims.Admin, ResourceKeysOperationClaims.Read };
    public class GetByIdResourceKeyQueryHandler : IRequestHandler<GetByIdResourceKeyQuery, GetByIdResourceKeyResponse>
    {
        private readonly IResourceKeyRepository _repository; private readonly IMapper _mapper; private readonly ResourceKeyBusinessRules _rules;
        public GetByIdResourceKeyQueryHandler(IResourceKeyRepository repository, IMapper mapper, ResourceKeyBusinessRules rules) { _repository = repository; _mapper = mapper; _rules = rules; }
        public async Task<GetByIdResourceKeyResponse> Handle(GetByIdResourceKeyQuery request, CancellationToken cancellationToken)
        {
            ResourceKey? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.ResourceKeyShouldExistWhenSelected(entity);
            return _mapper.Map<GetByIdResourceKeyResponse>(entity);
        }
    }
}
