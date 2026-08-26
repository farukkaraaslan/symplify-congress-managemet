using AutoMapper;
using Core.Application.Pipelines.Authorization;
using MediatR;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Constants;
using Symplify.BackOffice.Application.Features.OrganizationApiKeys.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.OrganizationApiKeys.Queries.GetById;

public class GetByIdOrganizationApiKeyQuery : IRequest<GetByIdOrganizationApiKeyResponse>, ISecuredRequest
{
    public Guid Id { get; set; }

    public string[] Roles => new[]
    {
        OrganizationApiKeysOperationClaims.Admin,
        OrganizationApiKeysOperationClaims.Read
    };

    public class GetByIdOrganizationApiKeyQueryHandler : IRequestHandler<GetByIdOrganizationApiKeyQuery, GetByIdOrganizationApiKeyResponse>
    {
        private readonly IOrganizationApiKeyRepository _repository;
        private readonly IMapper _mapper;
        private readonly OrganizationApiKeyBusinessRules _rules;

        public GetByIdOrganizationApiKeyQueryHandler(
            IOrganizationApiKeyRepository repository,
            IMapper mapper,
            OrganizationApiKeyBusinessRules rules)
        {
            _repository = repository;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<GetByIdOrganizationApiKeyResponse> Handle(GetByIdOrganizationApiKeyQuery request, CancellationToken cancellationToken)
        {
            await _rules.OrganizationApiKeyIdShouldBeValid(request.Id);

            OrganizationApiKey? entity = await _repository.GetAsync(
                predicate: apiKey => apiKey.Id == request.Id,
                cancellationToken: cancellationToken);

            await _rules.OrganizationApiKeyShouldExistWhenSelected(entity);

            return _mapper.Map<GetByIdOrganizationApiKeyResponse>(entity);
        }
    }
}
