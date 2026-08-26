using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Storage;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.Organizations.Constants;
using Symplify.BackOffice.Application.Features.Organizations.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Organization;

namespace Symplify.BackOffice.Application.Features.Organizations.Commands.Delete;

public class DeleteOrganizationCommand : IRequest<DeletedOrganizationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetOrganizations";
    public string[] Roles => new[] { OrganizationsOperationClaims.Admin, OrganizationsOperationClaims.Write, OrganizationsOperationClaims.Delete };

    public class DeleteOrganizationCommandHandler : IRequestHandler<DeleteOrganizationCommand, DeletedOrganizationResponse>
    {
        private readonly IOrganizationRepository _repository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IMapper _mapper;
        private readonly OrganizationBusinessRules _rules;

        public DeleteOrganizationCommandHandler(
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

        public async Task<DeletedOrganizationResponse> Handle(DeleteOrganizationCommand request, CancellationToken cancellationToken)
        {
            await _rules.OrganizationIdShouldBeValid(request.Id);

            Organization? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.OrganizationShouldExistWhenSelected(entity);
            await _rules.OrganizationShouldNotHaveRelatedCongressesWhenDeleting(request.Id);
            await _rules.OrganizationShouldNotHaveRelatedUsersWhenDeleting(request.Id);

            string? logoLightPath = entity!.LogoLightPath;
            string? logoDarkPath = entity.LogoDarkPath;

            Organization deletedEntity = await _repository.DeleteAsync(entity);

            string bucketName = GetCongressImagesBucketName();
            await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(_objectStorageService, bucketName, logoLightPath, cancellationToken);
            await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(_objectStorageService, bucketName, logoDarkPath, cancellationToken);

            return _mapper.Map<DeletedOrganizationResponse>(deletedEntity);
        }

        private string GetCongressImagesBucketName()
        {
            if (string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages))
                throw new InvalidOperationException(OrganizationsMessages.ObjectStorageBucketMissing);

            return _storageOptions.Buckets.CongressImages.Trim();
        }
    }
}
