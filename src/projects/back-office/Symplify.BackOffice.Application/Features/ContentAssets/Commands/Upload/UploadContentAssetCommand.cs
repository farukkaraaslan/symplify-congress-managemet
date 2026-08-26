using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Core.Application.Pipelines.Authorization;
using Core.Application.Storage;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Features.Congresses.Constants;
using Symplify.BackOffice.Application.Features.ContentAssets.Commands;
using Symplify.BackOffice.Application.Features.ContentAssets.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.ContentAssets.Commands.Upload;

public sealed partial class UploadContentAssetCommand : IRequest<UploadedContentAssetResponse>, ISecuredRequest
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx",
        ".txt",
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    public Guid? CongressId { get; set; }
    public ContentAssetFileInputDto? File { get; set; }

    public string[] Roles => new[]
    {
        CongressesOperationClaims.Admin,
        CongressesOperationClaims.Write,
        CongressesOperationClaims.Add,
        CongressesOperationClaims.Update
    };

    public sealed class UploadContentAssetCommandHandler
        : IRequestHandler<UploadContentAssetCommand, UploadedContentAssetResponse>
    {
        private readonly ICongressRepository _congressRepository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;

        public UploadContentAssetCommandHandler(
            ICongressRepository congressRepository,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions)
        {
            _congressRepository = congressRepository;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
        }

        public async Task<UploadedContentAssetResponse> Handle(
            UploadContentAssetCommand request,
            CancellationToken cancellationToken)
        {
            ValidateFile(request.File);

            ContentAssetFileInputDto file = request.File!;
            Congress? congress = null;

            if (request.CongressId.HasValue && request.CongressId.Value != Guid.Empty)
            {
                congress = await _congressRepository.GetAsync(
                    predicate: entity => entity.Id == request.CongressId.Value,
                    cancellationToken: cancellationToken);

                if (congress is null)
                    throw new BusinessException(ContentAssetsMessages.CongressNotFound);
            }

            string bucketName = GetCongressDocumentsBucketName();
            Guid assetId = Guid.NewGuid();
            string safeOriginalFileName = BuildSafeFileName(file.OriginalFileName, assetId);
            string objectName = BuildObjectName(congress, assetId, safeOriginalFileName);

            ObjectStorageUploadResult uploadResult = await _objectStorageService.UploadAsync(
                new ObjectStorageUploadRequest
                {
                    BucketName = bucketName,
                    ObjectName = objectName,
                    OriginalFileName = safeOriginalFileName,
                    ContentType = NormalizeContentType(file.ContentType, safeOriginalFileName),
                    Size = file.Length,
                    Content = file.Content,
                    Metadata = BuildMetadata(congress, assetId, safeOriginalFileName)
                },
                cancellationToken);

            return new UploadedContentAssetResponse
            {
                BucketName = uploadResult.BucketName,
                ObjectName = uploadResult.ObjectName,
                OriginalFileName = safeOriginalFileName,
                ContentType = uploadResult.ContentType,
                FileExtension = Path.GetExtension(safeOriginalFileName)?.ToLowerInvariant(),
                FileSize = uploadResult.Size,
                ETag = uploadResult.ETag
            };
        }

        private string GetCongressDocumentsBucketName()
        {
            if (string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressDocuments))
                throw new InvalidOperationException(ContentAssetsMessages.ObjectStorageBucketMissing);

            return _storageOptions.Buckets.CongressDocuments.Trim();
        }

        private static void ValidateFile(ContentAssetFileInputDto? file)
        {
            if (file is null || file.Length <= 0 || string.IsNullOrWhiteSpace(file.OriginalFileName))
                throw new BusinessException(ContentAssetsMessages.FileRequired);

            if (file.Length > MaxFileSizeBytes)
                throw new BusinessException(ContentAssetsMessages.FileTooLarge);

            string extension = Path.GetExtension(file.OriginalFileName);

            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
                throw new BusinessException(ContentAssetsMessages.FileInvalid);
        }

        private static string BuildObjectName(Congress? congress, Guid assetId, string safeFileName)
        {
            if (congress is null)
            {
                return JoinObjectName(
                    "backoffice",
                    "congress-content-assets",
                    "drafts",
                    DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
                    assetId.ToString("N"),
                    safeFileName);
            }

            return JoinObjectName(
                "backoffice",
                "organizations",
                congress.OrganizationId.ToString("N"),
                "congresses",
                congress.Id.ToString("N"),
                "content-assets",
                assetId.ToString("N"),
                safeFileName);
        }

        private static Dictionary<string, string> BuildMetadata(Congress? congress, Guid assetId, string safeFileName)
        {
            Dictionary<string, string> metadata = new(StringComparer.OrdinalIgnoreCase)
            {
                ["module"] = "congress-content-assets",
                ["asset-id"] = assetId.ToString("N"),
                ["original-file-name"] = safeFileName
            };

            if (congress is not null)
            {
                metadata["organization-id"] = congress.OrganizationId.ToString("N");
                metadata["congress-id"] = congress.Id.ToString("N");
            }

            return metadata;
        }

        private static string BuildSafeFileName(string originalFileName, Guid assetId)
        {
            string extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            string normalizedName = RemoveDiacritics(nameWithoutExtension).ToLowerInvariant();
            string safeName = InvalidFileNameCharactersRegex().Replace(normalizedName, "-");
            safeName = MultipleDashRegex().Replace(safeName, "-").Trim('-', '.', ' ');

            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "content-asset";

            return $"{safeName}-{assetId:N}{extension}";
        }

        private static string NormalizeContentType(string? contentType, string fileName)
        {
            if (!string.IsNullOrWhiteSpace(contentType))
                return contentType.Trim();

            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        private static string JoinObjectName(params string[] segments)
        {
            return string.Join(
                '/',
                segments
                    .Where(segment => !string.IsNullOrWhiteSpace(segment))
                    .Select(segment => segment.Trim().Trim('/').Replace('\\', '/')));
        }

        private static string RemoveDiacritics(string value)
        {
            string normalized = value.Normalize(NormalizationForm.FormD);
            StringBuilder builder = new();

            foreach (char character in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);

                if (category != UnicodeCategory.NonSpacingMark)
                    builder.Append(character);
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }

    [GeneratedRegex("[^a-z0-9._-]+", RegexOptions.Compiled)]
    private static partial Regex InvalidFileNameCharactersRegex();

    [GeneratedRegex("-{2,}", RegexOptions.Compiled)]
    private static partial Regex MultipleDashRegex();
}
