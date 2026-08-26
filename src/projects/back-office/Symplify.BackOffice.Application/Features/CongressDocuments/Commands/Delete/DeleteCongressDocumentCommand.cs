using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Storage;
using MediatR;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.CongressDocuments.Constants;
using Symplify.BackOffice.Application.Features.CongressDocuments.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressDocuments.Commands.Delete;

public class DeleteCongressDocumentCommand
    : IRequest<DeletedCongressDocumentResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public bool BypassCache { get; }

    public string? CacheKey { get; }

    public string CacheGroupKey => "GetCongressDocuments";

    public string[] Roles => new[]
    {
        CongressDocumentsOperationClaims.Admin,
        CongressDocumentsOperationClaims.Write,
        CongressDocumentsOperationClaims.Delete
    };

    public class DeleteCongressDocumentCommandHandler
        : IRequestHandler<DeleteCongressDocumentCommand, DeletedCongressDocumentResponse>
    {
        private readonly ICongressDocumentRepository _repository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly IMapper _mapper;
        private readonly CongressDocumentBusinessRules _rules;

        public DeleteCongressDocumentCommandHandler(
            ICongressDocumentRepository repository,
            IObjectStorageService objectStorageService,
            IMapper mapper,
            CongressDocumentBusinessRules rules)
        {
            _repository = repository;
            _objectStorageService = objectStorageService;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedCongressDocumentResponse> Handle(
            DeleteCongressDocumentCommand request,
            CancellationToken cancellationToken)
        {
            CongressDocument? entity = await _repository.GetAsync(
                predicate: item => item.Id.Equals(request.Id),
                cancellationToken: cancellationToken);

            await _rules.CongressDocumentShouldExistWhenSelected(entity);

            Guid congressId = entity!.CongressId;

            await DeleteObjectStorageFileIfExistsAsync(
                entity,
                cancellationToken);

            await DeleteCoverImageIfExistsAsync(
                entity,
                cancellationToken);

            CongressDocument deletedEntity = await _repository.DeleteAsync(entity);

            await NormalizeVisibleOrdersAsync(
                congressId,
                request.Id,
                cancellationToken);

            return _mapper.Map<DeletedCongressDocumentResponse>(deletedEntity);
        }

        private async Task DeleteObjectStorageFileIfExistsAsync(
            CongressDocument entity,
            CancellationToken cancellationToken)
        {
            string? bucketName = NormalizeStorageValue(entity.BucketName);
            string? objectName = NormalizeStorageValue(entity.ObjectName)
                ?? NormalizeStorageValue(entity.FilePath);

            if (string.IsNullOrWhiteSpace(bucketName) ||
                string.IsNullOrWhiteSpace(objectName))
            {
                return;
            }

            await _objectStorageService.DeleteAsync(
                new ObjectStorageDeleteRequest
                {
                    BucketName = bucketName,
                    ObjectName = objectName
                },
                cancellationToken);
        }


        private async Task DeleteCoverImageIfExistsAsync(
            CongressDocument entity,
            CancellationToken cancellationToken)
        {
            string? bucketName = NormalizeStorageValue(entity.CoverImageBucketName);
            string? objectName = NormalizeStorageValue(entity.CoverImageObjectName)
                ?? NormalizeStorageValue(entity.CoverImagePath);

            if (string.IsNullOrWhiteSpace(bucketName) ||
                string.IsNullOrWhiteSpace(objectName) ||
                IsExternalOrLegacyLocalPath(objectName))
            {
                return;
            }

            try
            {
                await _objectStorageService.DeleteAsync(
                    new ObjectStorageDeleteRequest
                    {
                        BucketName = bucketName,
                        ObjectName = objectName
                    },
                    cancellationToken);
            }
            catch
            {
                // Cover cleanup is best-effort. Deleting the document should not fail for an already removed image.
            }
        }

        private async Task NormalizeVisibleOrdersAsync(
            Guid congressId,
            Guid deletedEntityId,
            CancellationToken cancellationToken)
        {
            List<CongressDocument> entities = _repository
                .Query()
                .ToList()
                .Where(entity =>
                    entity.CongressId == congressId &&
                    !IsDeleted(entity) &&
                    entity.Id != deletedEntityId)
                .OrderBy(entity => entity.Order <= 0 ? int.MaxValue : entity.Order)
                .ThenBy(entity => entity.Id)
                .ToList();

            for (int index = 0; index < entities.Count; index++)
            {
                int normalizedOrder = index + 1;

                if (entities[index].Order == normalizedOrder)
                    continue;

                entities[index].Order = normalizedOrder;

                await _repository.UpdateAsync(entities[index]);
            }
        }

        private static string? NormalizeStorageValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static bool IsExternalOrLegacyLocalPath(string path)
        {
            return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("/", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("~/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDeleted(object entity)
        {
            object? deletedDate = LocalizedEntityRuntimeHelper.GetPropertyValue(
                entity,
                "DeletedDate");

            return deletedDate is not null;
        }
    }
}
