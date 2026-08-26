using Core.Application.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Services.Maintenance;
using Symplify.BackOffice.Application.Services.Storage;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Services.Maintenance;

public sealed class CongressCleanupService : ICongressCleanupService
{
    private readonly BackOfficeDbContext _context;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IObjectStoragePrefixCleanupService _prefixCleanupService;
    private readonly ObjectStorageOptions _storageOptions;

    public CongressCleanupService(
        BackOfficeDbContext context,
        IObjectStorageService objectStorageService,
        IObjectStoragePrefixCleanupService prefixCleanupService,
        IOptions<ObjectStorageOptions> storageOptions)
    {
        _context = context;
        _objectStorageService = objectStorageService;
        _prefixCleanupService = prefixCleanupService;
        _storageOptions = storageOptions.Value;
    }

    public async Task<CongressDeleteInspectionResult> InspectDocumentOnlyCleanupAsync(
        Guid congressId,
        CancellationToken cancellationToken = default)
    {
        Congress? congress = await _context.Congresses
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == congressId && entity.DeletedDate == null, cancellationToken);

        if (congress is null)
            throw new InvalidOperationException("Kongre bulunamadı veya daha önce silinmiş.");

        List<Guid> documentIds = await _context.CongressDocuments
            .AsNoTracking()
            .Where(entity => entity.CongressId == congressId && entity.DeletedDate == null)
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);

        Dictionary<string, int> blockingDependencies = new(StringComparer.OrdinalIgnoreCase);

        if (congress.Status != CongressStatus.Archived)
        {
            blockingDependencies["Kongre durumu arşivde değil"] = 1;
        }

        await AddBlockingDependencyAsync(blockingDependencies, "Bildiri", _context.Submissions.CountAsync(entity => entity.CongressId == congressId && entity.DeletedDate == null, cancellationToken));
        await AddBlockingDependencyAsync(blockingDependencies, "Ödeme belgesi", _context.PaymentDocuments.CountAsync(entity => entity.CongressId == congressId && entity.DeletedDate == null, cancellationToken));
        await AddBlockingDependencyAsync(blockingDependencies, "Slider", _context.CongressSliders.CountAsync(entity => entity.CongressId == congressId && entity.DeletedDate == null, cancellationToken));
        await AddBlockingDependencyAsync(blockingDependencies, "Sayfa bölümü", _context.CongressSections.CountAsync(entity => entity.CongressId == congressId && entity.DeletedDate == null, cancellationToken));
        await AddBlockingDependencyAsync(blockingDependencies, "Duyuru", _context.CongressAnnouncements.CountAsync(entity => entity.CongressId == congressId && entity.DeletedDate == null, cancellationToken));
        await AddBlockingDependencyAsync(blockingDependencies, "Kurul", _context.CongressBoards.CountAsync(entity => entity.CongressId == congressId && entity.DeletedDate == null, cancellationToken));
        await AddBlockingDependencyAsync(blockingDependencies, "Önemli tarih", _context.CongressImportantDates.CountAsync(entity => entity.CongressId == congressId && entity.DeletedDate == null, cancellationToken));
        await AddBlockingDependencyAsync(blockingDependencies, "Ödeme planı", _context.CongressPaymentPlans.CountAsync(entity => entity.CongressId == congressId && entity.DeletedDate == null, cancellationToken));
        await AddBlockingDependencyAsync(blockingDependencies, "Konu", _context.CongressTopics.CountAsync(entity => entity.CongressId == congressId && entity.DeletedDate == null, cancellationToken));
        await AddBlockingDependencyAsync(blockingDependencies, "Bildiri türü", _context.CongressSubmissionTypes.CountAsync(entity => entity.CongressId == congressId && entity.DeletedDate == null, cancellationToken));
        await AddBlockingDependencyAsync(blockingDependencies, "Değerlendirme kriteri", _context.CongressEvaluationCriteria.CountAsync(entity => entity.CongressId == congressId && entity.DeletedDate == null, cancellationToken));
        await AddBlockingDependencyAsync(blockingDependencies, "Hakem ataması", _context.CongressReviewers.CountAsync(entity => entity.CongressId == congressId && entity.DeletedDate == null, cancellationToken));
        await AddBlockingDependencyAsync(blockingDependencies, "Program planı", _context.CongressProgramPlans.CountAsync(entity => entity.CongressId == congressId && entity.DeletedDate == null, cancellationToken));

        int documentTranslationCount = documentIds.Count == 0
            ? 0
            : await _context.CongressDocumentTranslations.CountAsync(entity => documentIds.Contains(entity.CongressDocumentId), cancellationToken);

        return new CongressDeleteInspectionResult
        {
            CongressId = congress.Id,
            Code = congress.Code,
            Title = !string.IsNullOrWhiteSpace(congress.Name) ? congress.Name : congress.Code,
            Status = congress.Status.ToString(),
            DocumentCount = documentIds.Count,
            DocumentTranslationCount = documentTranslationCount,
            TranslationCount = await _context.CongressTranslations.CountAsync(entity => entity.CongressId == congressId, cancellationToken),
            WorkflowSettingCount = await _context.CongressWorkflowSettings.CountAsync(entity => entity.CongressId == congressId, cancellationToken),
            WorkflowTransitionCount = await _context.CongressTransactionStatusTransitions.CountAsync(entity => entity.CongressId == congressId, cancellationToken),
            BlockingDependencies = blockingDependencies
        };
    }

    public async Task<CongressDeleteCleanupResult> DeleteDocumentOnlyCongressAsync(
        Guid congressId,
        CancellationToken cancellationToken = default)
    {
        CongressDeleteInspectionResult inspection = await InspectDocumentOnlyCleanupAsync(congressId, cancellationToken);

        if (!inspection.IsSafeForDocumentOnlyDelete)
        {
            string details = string.Join(
                ", ",
                inspection.BlockingDependencies.Select(item => $"{item.Key}: {item.Value}"));

            throw new InvalidOperationException($"Bu kongre kontrollü doküman temizliği için uygun değil. Bağlı kayıtlar: {details}");
        }

        Congress congress = await _context.Congresses
            .AsNoTracking()
            .FirstAsync(entity => entity.Id == congressId, cancellationToken);

        List<CongressDocument> documents = await _context.CongressDocuments
            .AsNoTracking()
            .Where(entity => entity.CongressId == congressId)
            .ToListAsync(cancellationToken);

        HashSet<string> deletedStorageObjects = new(StringComparer.Ordinal);

        await DeleteKnownStorageObjectsAsync(congress, documents, deletedStorageObjects, cancellationToken);
        await DeleteCongressStoragePrefixesAsync(congress, deletedStorageObjects, cancellationToken);

        int documentCount = documents.Count;
        int documentTranslationCount = 0;
        int congressTranslationCount = 0;
        int workflowRecordCount = 0;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        List<Guid> documentIds = documents.Select(entity => entity.Id).ToList();

        if (documentIds.Count > 0)
        {
            documentTranslationCount = await _context.CongressDocumentTranslations
                .Where(entity => documentIds.Contains(entity.CongressDocumentId))
                .ExecuteDeleteAsync(cancellationToken);

            await _context.CongressDocuments
                .Where(entity => entity.CongressId == congressId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        workflowRecordCount += await _context.CongressTransactionStatusTransitions
            .Where(entity => entity.CongressId == congressId)
            .ExecuteDeleteAsync(cancellationToken);

        workflowRecordCount += await _context.CongressWorkflowSettings
            .Where(entity => entity.CongressId == congressId)
            .ExecuteDeleteAsync(cancellationToken);

        congressTranslationCount = await _context.CongressTranslations
            .Where(entity => entity.CongressId == congressId)
            .ExecuteDeleteAsync(cancellationToken);

        await _context.Congresses
            .Where(entity => entity.Id == congressId)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new CongressDeleteCleanupResult
        {
            CongressId = congress.Id,
            Code = congress.Code,
            Title = !string.IsNullOrWhiteSpace(congress.Name) ? congress.Name : congress.Code,
            DeletedDocumentCount = documentCount,
            DeletedTranslationCount = documentTranslationCount + congressTranslationCount,
            DeletedWorkflowRecordCount = workflowRecordCount,
            DeletedStorageObjectCount = deletedStorageObjects.Count,
            DeletedStorageObjects = deletedStorageObjects.ToList()
        };
    }

    private static async Task AddBlockingDependencyAsync(
        IDictionary<string, int> target,
        string label,
        Task<int> countTask)
    {
        int count = await countTask;

        if (count > 0)
            target[label] = count;
    }

    private async Task DeleteKnownStorageObjectsAsync(
        Congress congress,
        IEnumerable<CongressDocument> documents,
        ISet<string> deletedStorageObjects,
        CancellationToken cancellationToken)
    {
        await DeleteObjectIfExistsAsync(GetCongressImagesBucketName(), congress.LogoLightPath, deletedStorageObjects, cancellationToken);
        await DeleteObjectIfExistsAsync(GetCongressImagesBucketName(), congress.LogoDarkPath, deletedStorageObjects, cancellationToken);

        foreach (CongressDocument document in documents)
        {
            await DeleteObjectIfExistsAsync(document.BucketName, document.ObjectName ?? document.FilePath, deletedStorageObjects, cancellationToken);
            await DeleteObjectIfExistsAsync(document.CoverImageBucketName, document.CoverImageObjectName ?? document.CoverImagePath, deletedStorageObjects, cancellationToken);
        }
    }

    private async Task DeleteCongressStoragePrefixesAsync(
        Congress congress,
        ISet<string> deletedStorageObjects,
        CancellationToken cancellationToken)
    {
        string? documentsBucketName = NormalizeBucketName(_storageOptions.Buckets.CongressDocuments);
        string? imagesBucketName = NormalizeBucketName(_storageOptions.Buckets.CongressImages);

        string[] documentPrefixes =
        {
            $"backoffice/organizations/{congress.OrganizationId:N}/congresses/{congress.Id:N}/",
            $"backoffice/organizations/{congress.OrganizationId:D}/congresses/{congress.Id:D}/"
        };

        string[] imagePrefixes =
        {
            $"backoffice/congresses/{congress.Id:N}/",
            $"backoffice/congresses/{congress.Id:D}/",
            $"backoffice/organizations/{congress.OrganizationId:N}/congresses/{congress.Id:N}/",
            $"backoffice/organizations/{congress.OrganizationId:D}/congresses/{congress.Id:D}/"
        };

        foreach (string prefix in documentPrefixes)
        {
            await DeletePrefixIfBucketExistsAsync(documentsBucketName, prefix, deletedStorageObjects, cancellationToken);
        }

        foreach (string prefix in imagePrefixes)
        {
            await DeletePrefixIfBucketExistsAsync(imagesBucketName, prefix, deletedStorageObjects, cancellationToken);
        }
    }

    private async Task DeletePrefixIfBucketExistsAsync(
        string? bucketName,
        string prefix,
        ISet<string> deletedStorageObjects,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            return;

        IReadOnlyList<string> deletedObjects = await _prefixCleanupService.DeletePrefixAsync(
            bucketName,
            prefix,
            cancellationToken);

        foreach (string objectName in deletedObjects)
        {
            deletedStorageObjects.Add(BuildStorageKey(bucketName, objectName));
        }
    }

    private async Task DeleteObjectIfExistsAsync(
        string? bucketName,
        string? objectName,
        ISet<string> deletedStorageObjects,
        CancellationToken cancellationToken)
    {
        string? normalizedBucketName = NormalizeBucketName(bucketName);
        string? normalizedObjectName = NormalizeObjectName(objectName);

        if (string.IsNullOrWhiteSpace(normalizedBucketName) || string.IsNullOrWhiteSpace(normalizedObjectName))
            return;

        await _objectStorageService.DeleteAsync(
            new ObjectStorageDeleteRequest
            {
                BucketName = normalizedBucketName,
                ObjectName = normalizedObjectName
            },
            cancellationToken);

        deletedStorageObjects.Add(BuildStorageKey(normalizedBucketName, normalizedObjectName));
    }

    private string GetCongressImagesBucketName()
        => NormalizeBucketName(_storageOptions.Buckets.CongressImages) ?? string.Empty;

    private static string? NormalizeBucketName(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().Trim('/');

    private static string? NormalizeObjectName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string normalized = value.Trim().Trim('/').Replace('\\', '/');

        if (normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("~/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string BuildStorageKey(string bucketName, string objectName)
        => $"{bucketName.Trim().Trim('/')}/{objectName.Trim().Trim('/')}";
}
