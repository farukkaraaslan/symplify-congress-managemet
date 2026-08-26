using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.Application.Storage;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands;
using Symplify.BackOffice.Application.Features.CongressBoardMembers.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.CongressBoardMembers.Commands.UpdateAcceptanceSignature;

public sealed class UpdateCongressBoardMemberAcceptanceSignatureCommand : IRequest, ISecuredRequest, ICacheRemoverRequest
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public bool IsAcceptanceLetterSigner { get; set; }
    public CongressBoardMemberSignatureInputDto? Signature { get; set; }

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetCongressBoardMembers";
    public string[] Roles =>
    [
        CongressBoardMembersOperationClaims.Admin,
        CongressBoardMembersOperationClaims.Write,
        CongressBoardMembersOperationClaims.Update
    ];

    public sealed class Handler : IRequestHandler<UpdateCongressBoardMemberAcceptanceSignatureCommand>
    {
        private static readonly HashSet<string> AllowedSignatureExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png"
        };

        private static readonly Regex InvalidCharactersRegex = new(
            "[^a-z0-9._-]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex MultipleDashRegex = new(
            "-{2,}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly ICongressBoardRepository _boardRepository;
        private readonly ICongressBoardMemberRepository _memberRepository;
        private readonly IObjectStorageService _objectStorageService;
        private readonly ObjectStorageOptions _storageOptions;

        public Handler(
            ICongressBoardRepository boardRepository,
            ICongressBoardMemberRepository memberRepository,
            IObjectStorageService objectStorageService,
            IOptions<ObjectStorageOptions> storageOptions)
        {
            _boardRepository = boardRepository;
            _memberRepository = memberRepository;
            _objectStorageService = objectStorageService;
            _storageOptions = storageOptions.Value;
        }

        public async Task Handle(UpdateCongressBoardMemberAcceptanceSignatureCommand request, CancellationToken cancellationToken)
        {
            CongressBoardMember? member = await _memberRepository
                .Query()
                .FirstOrDefaultAsync(item => item.Id == request.Id && item.DeletedDate == null, cancellationToken);

            if (member is null)
                throw new InvalidOperationException(CongressBoardMembersMessages.EntityNotFound);

            CongressBoard? board = await _boardRepository
                .Query()
                .FirstOrDefaultAsync(item => item.Id == member.CongressBoardId && item.DeletedDate == null, cancellationToken);

            if (board is null || board.CongressId != request.CongressId)
                throw new InvalidOperationException(CongressBoardMembersMessages.EntityNotFound);

            string? oldSignatureBucketName = member.SignatureBucketName;
            string? oldSignatureObjectName = !string.IsNullOrWhiteSpace(member.SignatureObjectName)
                ? member.SignatureObjectName
                : member.SignaturePath;

            bool signatureReplaced = false;

            if (request.Signature is not null && request.Signature.Length > 0)
            {
                await UploadSignatureAsync(
                    member,
                    board.CongressId,
                    request.Signature,
                    cancellationToken);

                signatureReplaced = true;
            }

            if (request.IsAcceptanceLetterSigner)
            {
                if (!HasSignature(member))
                    throw new InvalidOperationException(CongressBoardMembersMessages.SignatureRequiredForSigner);

                await ClearOtherSignersAsync(board.CongressId, member.Id, cancellationToken);
            }

            member.IsAcceptanceLetterSigner = request.IsAcceptanceLetterSigner;

            await _memberRepository.UpdateAsync(member);

            if (signatureReplaced)
            {
                string? newSignatureObjectName =
                    !string.IsNullOrWhiteSpace(member.SignatureObjectName)
                        ? member.SignatureObjectName
                        : member.SignaturePath;

                if (!IsSameStorageObject(
                        oldSignatureBucketName,
                        oldSignatureObjectName,
                        member.SignatureBucketName,
                        newSignatureObjectName))
                {
                    await DeleteSignatureIfExistsAsync(
                        oldSignatureBucketName,
                        oldSignatureObjectName,
                        cancellationToken);
                }
            }
        }

        private async Task ClearOtherSignersAsync(Guid congressId, Guid selectedMemberId, CancellationToken cancellationToken)
        {
            List<Guid> boardIds = await _boardRepository
                .Query()
                .Where(board => board.CongressId == congressId && board.DeletedDate == null)
                .Select(board => board.Id)
                .ToListAsync(cancellationToken);

            if (boardIds.Count == 0)
                return;

            List<CongressBoardMember> otherSigners = await _memberRepository
                .Query()
                .Where(member =>
                    member.Id != selectedMemberId &&
                    member.IsAcceptanceLetterSigner &&
                    member.DeletedDate == null &&
                    boardIds.Contains(member.CongressBoardId))
                .ToListAsync(cancellationToken);

            foreach (CongressBoardMember signer in otherSigners)
            {
                signer.IsAcceptanceLetterSigner = false;
                await _memberRepository.UpdateAsync(signer);
            }
        }

        private async Task UploadSignatureAsync(
            CongressBoardMember member,
            Guid congressId,
            CongressBoardMemberSignatureInputDto signature,
            CancellationToken cancellationToken)
        {
            string extension = Path.GetExtension(signature.OriginalFileName);
            if (!AllowedSignatureExtensions.Contains(extension))
                throw new InvalidOperationException("BackOffice.CongressBoardMembers.Validation.SignatureExtensionInvalid");

            string bucketName = GetCongressImagesBucketName();
            string fileName = $"signature-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            string objectName = string.Join('/', new[]
            {
                "congresses",
                congressId.ToString("N"),
                "board-members",
                member.Id.ToString("N"),
                "signature",
                Slug(fileName)
            });

            ObjectStorageUploadResult uploadResult = await _objectStorageService.UploadAsync(
                new ObjectStorageUploadRequest
                {
                    BucketName = bucketName,
                    ObjectName = objectName,
                    OriginalFileName = fileName,
                    ContentType = NormalizeContentType(signature.ContentType),
                    Size = signature.Length,
                    Content = signature.Content,
                    Metadata = new Dictionary<string, string>
                    {
                        ["module"] = "congress-board-member-signature",
                        ["congress-id"] = congressId.ToString("D"),
                        ["board-member-id"] = member.Id.ToString("D")
                    }
                },
                cancellationToken);

            member.SignaturePath = uploadResult.ObjectName;
            member.SignatureStorageProvider = _storageOptions.Provider;
            member.SignatureBucketName = uploadResult.BucketName;
            member.SignatureObjectName = uploadResult.ObjectName;
            member.SignatureFileName = uploadResult.OriginalFileName;
            member.SignatureContentType = uploadResult.ContentType;
            member.SignatureFileSize = uploadResult.Size;
            member.SignatureETag = uploadResult.ETag;
        }

        private static bool HasSignature(CongressBoardMember member)
            => !string.IsNullOrWhiteSpace(member.SignatureObjectName) || !string.IsNullOrWhiteSpace(member.SignaturePath);

        private bool IsSameStorageObject(
            string? firstBucketName,
            string? firstObjectName,
            string? secondBucketName,
            string? secondObjectName)
        {
            if (string.IsNullOrWhiteSpace(firstObjectName) ||
                string.IsNullOrWhiteSpace(secondObjectName))
            {
                return false;
            }

            if (IsExternalOrLegacyLocalPath(firstObjectName) ||
                IsExternalOrLegacyLocalPath(secondObjectName))
            {
                return string.Equals(
                    firstObjectName.Trim(),
                    secondObjectName.Trim(),
                    StringComparison.OrdinalIgnoreCase);
            }

            string firstBucket = !string.IsNullOrWhiteSpace(firstBucketName)
                ? firstBucketName.Trim()
                : GetCongressImagesBucketName();

            string secondBucket = !string.IsNullOrWhiteSpace(secondBucketName)
                ? secondBucketName.Trim()
                : GetCongressImagesBucketName();

            return string.Equals(
                       firstBucket,
                       secondBucket,
                       StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(
                       firstObjectName.Trim(),
                       secondObjectName.Trim(),
                       StringComparison.Ordinal);
        }

        private async Task DeleteSignatureIfExistsAsync(
            string? bucketName,
            string? objectName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(objectName) ||
                IsExternalOrLegacyLocalPath(objectName))
            {
                return;
            }

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
                // DB update başarıyla tamamlandıktan sonra eski object cleanup
                // başarısız olursa kullanıcı işlemini bozmayalım.
                // Storage orphan cleanup daha sonra tekrar yapılabilir.
            }
        }

        private static bool IsExternalOrLegacyLocalPath(string path)
        {
            return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("/", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("~/", StringComparison.OrdinalIgnoreCase);
        }

        private string GetCongressImagesBucketName()
        {
            if (string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages))
                throw new InvalidOperationException(CongressBoardMembersMessages.ObjectStorageBucketMissing);

            return _storageOptions.Buckets.CongressImages.Trim();
        }

        private static string NormalizeContentType(string? contentType)
            => string.IsNullOrWhiteSpace(contentType) ? "image/png" : contentType.Trim();

        private static string Slug(string value)
        {
            string normalized = value.Trim().Normalize(NormalizationForm.FormD);
            StringBuilder builder = new();

            foreach (char character in normalized)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category != UnicodeCategory.NonSpacingMark)
                    builder.Append(character);
            }

            string ascii = builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
            string sanitized = InvalidCharactersRegex.Replace(ascii, "-");
            sanitized = MultipleDashRegex.Replace(sanitized, "-").Trim('-', '.', ' ');

            return string.IsNullOrWhiteSpace(sanitized) ? "signature.png" : sanitized;
        }
    }
}
