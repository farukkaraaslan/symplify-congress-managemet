using AutoMapper;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Storage;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Constants;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Rules;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.Delete;

public class DeleteCongressBoardMemberCommand : IRequest<DeletedCongressBoardMemberResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressBoardMembers";
    public string[] Roles => new[] { CongressBoardMembersOperationClaims.Admin, CongressBoardMembersOperationClaims.Write, CongressBoardMembersOperationClaims.Delete };

    public class DeleteCongressBoardMemberCommandHandler : IRequestHandler<DeleteCongressBoardMemberCommand, DeletedCongressBoardMemberResponse>
    {
        private readonly ICongressBoardMemberRepository _repository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;
        private readonly IMapper _mapper;
        private readonly CongressBoardMemberBusinessRules _rules;

        public DeleteCongressBoardMemberCommandHandler(
            ICongressBoardMemberRepository repository,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions,
            IMapper mapper,
            CongressBoardMemberBusinessRules rules)
        {
            _repository = repository;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
            _mapper = mapper;
            _rules = rules;
        }

        public async Task<DeletedCongressBoardMemberResponse> Handle(
            DeleteCongressBoardMemberCommand request,
            CancellationToken cancellationToken)
        {
            CongressBoardMember? entity = await _repository.GetAsync(predicate: x => x.Id.Equals(request.Id));
            await _rules.CongressBoardMemberShouldExistWhenSelected(entity);

            string? bucketName = entity!.ImageBucketName;
            string? objectName = !string.IsNullOrWhiteSpace(entity.ImageObjectName)
                ? entity.ImageObjectName
                : entity.ImagePath;

            CongressBoardMember deletedEntity = await _repository.DeleteAsync(entity);

            await DeleteImageIfExistsAsync(bucketName, objectName, cancellationToken);

            return _mapper.Map<DeletedCongressBoardMemberResponse>(deletedEntity);
        }

        private async Task DeleteImageIfExistsAsync(string? bucketName, string? objectName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(objectName) || IsExternalOrLegacyLocalPath(objectName))
                return;

            string effectiveBucketName = !string.IsNullOrWhiteSpace(bucketName)
                ? bucketName.Trim()
                : GetCongressImagesBucketName();

            try
            {
                await _objectStorageService.DeleteAsync(
                    new ObjectStorageDeleteRequest
                    {
                        BucketName = effectiveBucketName,
                        ObjectName = objectName.Trim()
                    },
                    cancellationToken);
            }
            catch
            {
                // Object may already be removed. DB delete is authoritative; storage cleanup is best-effort.
            }
        }

        private string GetCongressImagesBucketName()
        {
            if (string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages))
                throw new InvalidOperationException(CongressBoardMembersMessages.ObjectStorageBucketMissing);

            return _storageOptions.Buckets.CongressImages.Trim();
        }

        private static bool IsExternalOrLegacyLocalPath(string path)
        {
            return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("/", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("~/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
