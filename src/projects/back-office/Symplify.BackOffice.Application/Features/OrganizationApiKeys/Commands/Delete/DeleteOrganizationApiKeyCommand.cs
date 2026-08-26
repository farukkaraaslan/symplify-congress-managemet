using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using MediatR;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Constants;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.OrganizationApiKeys.Commands.Delete;

public class DeleteOrganizationApiKeyCommand : IRequest<DeletedOrganizationApiKeyResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetOrganizationApiKeys";

    public string[] Roles => new[]
    {
        OrganizationApiKeysOperationClaims.Admin,
        OrganizationApiKeysOperationClaims.Write,
        OrganizationApiKeysOperationClaims.Delete
    };

    public class DeleteOrganizationApiKeyCommandHandler : IRequestHandler<DeleteOrganizationApiKeyCommand, DeletedOrganizationApiKeyResponse>
    {
        private readonly IOrganizationApiKeyRepository _repository;
        private readonly IMapper _mapper;
        private readonly OrganizationApiKeyBusinessRules _rules;

        public DeleteOrganizationApiKeyCommandHandler(
            IOrganizationApiKeyRepository repository,
            IMapper mapper,
            OrganizationApiKeyBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedOrganizationApiKeyResponse> Handle(DeleteOrganizationApiKeyCommand request, CancellationToken cancellationToken)
        {
            await _rules.OrganizationApiKeyIdShouldBeValid(request.Id);

            OrganizationApiKey? entity = await _repository.GetAsync(
                predicate: apiKey => apiKey.Id == request.Id,
                cancellationToken: cancellationToken);

            await _rules.OrganizationApiKeyShouldExistWhenSelected(entity);

            OrganizationApiKey deletedEntity = await _repository.DeleteAsync(entity!);
            return _mapper.Map<DeletedOrganizationApiKeyResponse>(deletedEntity);
        }
    }
}
