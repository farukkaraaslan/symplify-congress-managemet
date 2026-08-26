using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Storage;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Features.Congresses.Constants;
using Symplify.BackOffice.Application.Features.Congresses.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.Congresses.Commands.Delete;

public class DeleteCongressCommand : IRequest<DeletedCongressResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongresses";
    public string[] Roles => new[] { CongressesOperationClaims.Admin, CongressesOperationClaims.Write, CongressesOperationClaims.Delete };

    public class DeleteCongressCommandHandler : IRequestHandler<DeleteCongressCommand, DeletedCongressResponse>
    {
        private readonly ICongressRepository _repository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IMapper _mapper;
        private readonly CongressBusinessRules _rules;

        public DeleteCongressCommandHandler(
            ICongressRepository repository,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions,
            IMapper mapper,
            CongressBusinessRules rules)
        {
            _repository = repository;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedCongressResponse> Handle(DeleteCongressCommand request, CancellationToken cancellationToken)
        {
            Congress? entity = await _repository.GetAsync(predicate: x => x.Id!.Equals(request.Id));
            await _rules.CongressShouldExistWhenSelected(entity);

            string? logoLightPath = entity!.LogoLightPath;
            string? logoDarkPath = entity.LogoDarkPath;

            Congress deletedEntity = await _repository.DeleteAsync(entity);

            string bucketName = GetCongressImagesBucketName();

            if (IsCongressOwnedLogoObject(logoLightPath, entity.Id))
                await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(_objectStorageService, bucketName, logoLightPath, cancellationToken);

            if (IsCongressOwnedLogoObject(logoDarkPath, entity.Id))
                await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(_objectStorageService, bucketName, logoDarkPath, cancellationToken);

            return _mapper.Map<DeletedCongressResponse>(deletedEntity);
        }


        private static bool IsCongressOwnedLogoObject(string? objectName, Guid congressId)
        {
            return !string.IsNullOrWhiteSpace(objectName)
                && objectName.Contains($"/congresses/{congressId:D}/logos/", StringComparison.OrdinalIgnoreCase);
        }

        private string GetCongressImagesBucketName()
        {
            if (string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages))
                throw new InvalidOperationException(CongressesMessages.ObjectStorageBucketMissing);

            return _storageOptions.Buckets.CongressImages.Trim();
        }
    }
}
