using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Storage;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.Organizations.Constants;
using Symplify.BackOffice.Application.Features.Organizations.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.Organizations.Queries.GetById;

public class GetByIdOrganizationQuery : IRequest<GetByIdOrganizationResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string[] Roles => new[] { OrganizationsOperationClaims.Admin, OrganizationsOperationClaims.Read };

    public class GetByIdOrganizationQueryHandler : IRequestHandler<GetByIdOrganizationQuery, GetByIdOrganizationResponse>
    {
        private readonly IOrganizationRepository _repository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IMapper _mapper;
        private readonly OrganizationBusinessRules _rules;

        public GetByIdOrganizationQueryHandler(
            IOrganizationRepository repository,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions,
            IMapper mapper,
            OrganizationBusinessRules rules)
        {
            _repository = repository;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<GetByIdOrganizationResponse> Handle(GetByIdOrganizationQuery request, CancellationToken cancellationToken)
        {
            Organization? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.OrganizationShouldExistWhenSelected(entity);

            GetByIdOrganizationResponse response = _mapper.Map<GetByIdOrganizationResponse>(entity);
            response.LogoLightUrl = await ResolveImageUrlAsync(response.LogoLightPath, cancellationToken);
            response.LogoDarkUrl = await ResolveImageUrlAsync(response.LogoDarkPath, cancellationToken);

            return response;
        }

        private async Task<string?> ResolveImageUrlAsync(string? objectName, CancellationToken cancellationToken)
        {
            return await BackOfficeObjectStorageHelper.GetReadUrlOrPathAsync(
                _objectStorageService,
                GetCongressImagesBucketNameOrNull(),
                objectName,
                TimeSpan.FromMinutes(10),
                cancellationToken);
        }

        private string? GetCongressImagesBucketNameOrNull()
        {
            return string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages)
                ? null
                : _storageOptions.Buckets.CongressImages.Trim();
        }
    }
}
