namespace Symplify.BackOffice.Application.Services.Maintenance;

public interface ICongressCleanupService
{
    Task<CongressDeleteInspectionResult> InspectDocumentOnlyCleanupAsync(
        Guid congressId,
        CancellationToken cancellationToken = default);

    Task<CongressDeleteCleanupResult> DeleteDocumentOnlyCongressAsync(
        Guid congressId,
        CancellationToken cancellationToken = default);
}

public sealed class CongressDeleteInspectionResult
{
    public Guid CongressId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int DocumentCount { get; init; }
    public int DocumentTranslationCount { get; init; }
    public int TranslationCount { get; init; }
    public int WorkflowSettingCount { get; init; }
    public int WorkflowTransitionCount { get; init; }
    public bool IsSafeForDocumentOnlyDelete => BlockingDependencies.Count == 0;
    public IReadOnlyDictionary<string, int> BlockingDependencies { get; init; } = new Dictionary<string, int>();
}

public sealed class CongressDeleteCleanupResult
{
    public Guid CongressId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int DeletedDocumentCount { get; init; }
    public int DeletedTranslationCount { get; init; }
    public int DeletedWorkflowRecordCount { get; init; }
    public int DeletedStorageObjectCount { get; init; }
    public IReadOnlyList<string> DeletedStorageObjects { get; init; } = Array.Empty<string>();
}
