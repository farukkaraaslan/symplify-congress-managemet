using Core.Application.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Features.AbstractBook.Models;
using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;

namespace Symplify.BackOffice.Application.Features.AbstractBook.Services;

public sealed class AbstractBookDocumentBuilder : IAbstractBookDocumentBuilder
{
    private const int MaxHeaderLogoSizeInBytes = 5 * 1024 * 1024;

    private readonly IProgramManagementRepository _programRepository;
    private readonly IAbstractBookRepository _abstractBookRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ObjectStorageOptions _storageOptions;
    private readonly ILogger<AbstractBookDocumentBuilder> _logger;

    public AbstractBookDocumentBuilder(
        IProgramManagementRepository programRepository,
        IAbstractBookRepository abstractBookRepository,
        IObjectStorageService objectStorageService,
        IOptions<ObjectStorageOptions> storageOptions,
        ILogger<AbstractBookDocumentBuilder> logger)
    {
        _programRepository = programRepository;
        _abstractBookRepository = abstractBookRepository;
        _objectStorageService = objectStorageService;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    public async Task<AbstractBookDocumentModel> BuildAsync(
        AbstractBookBuildRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Filter);
        ArgumentNullException.ThrowIfNull(request.Options);

        if (request.CongressId == Guid.Empty)
            throw new InvalidOperationException("Kongre seçimi zorunludur.");
        if (!request.Options.IncludeTurkishContent && !request.Options.IncludeEnglishContent)
            throw new InvalidOperationException("En az bir içerik dili seçilmelidir.");

        await EnsureHeaderLogoAsync(
            request.CongressId,
            request.Culture,
            request.Options,
            cancellationToken);

        ProgramGenerationSourceDto source = await _programRepository.GetGenerationSourceAsync(
            request.CongressId,
            null,
            request.Culture,
            cancellationToken,
            request.Filter)
            ?? throw new InvalidOperationException("Aktif kongre bulunamadı.");

        if (source.FilteredSubmissions.Count == 0)
            throw new InvalidOperationException("Seçilen filtrelere uygun bildiri bulunamadı.");

        Guid[] submissionIds = source.FilteredSubmissions
            .Select(x => x.Id)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        AbstractBookDocumentSourceDto detailedSource = await _abstractBookRepository.GetDocumentSourceAsync(
            request.CongressId,
            submissionIds,
            request.Culture,
            cancellationToken)
            ?? throw new InvalidOperationException("Özet kitabı verileri yüklenemedi.");

        IReadOnlyDictionary<Guid, ProgramSubmissionSourceDto> sourceById = source.FilteredSubmissions
            .GroupBy(x => x.Id)
            .ToDictionary(x => x.Key, x => x.First());

        List<AbstractBookEntryDto> entries = detailedSource.Submissions
            .Where(x => sourceById.ContainsKey(x.Id))
            .Select(content =>
            {
                ProgramSubmissionSourceDto metadata = sourceById[content.Id];
                return new AbstractBookEntryDto
                {
                    Id = content.Id,
                    SubmissionNumber = string.IsNullOrWhiteSpace(content.SubmissionNumber)
                        ? metadata.SubmissionNumber
                        : content.SubmissionNumber,
                    SubmissionTypeName = metadata.SubmissionTypeName,
                    TopicName = metadata.TopicName,
                    TurkishTitle = FirstNonEmpty(content.TurkishTitle, metadata.Title),
                    EnglishTitle = content.EnglishTitle,
                    TurkishAbstract = content.TurkishAbstract,
                    EnglishAbstract = content.EnglishAbstract,
                    TurkishKeywords = content.TurkishKeywords,
                    EnglishKeywords = content.EnglishKeywords,
                    Authors = content.Authors
                };
            })
            .ToList();

        IReadOnlyDictionary<Guid, int> programOrder = request.Options.SortMode == AbstractBookSortMode.ProgramOrder
            ? await BuildProgramOrderAsync(request.CongressId, cancellationToken)
            : new Dictionary<Guid, int>();

        entries = ApplySort(entries, request.Options.SortMode, programOrder);

        return new AbstractBookDocumentModel
        {
            CongressId = request.CongressId,
            CongressCode = detailedSource.CongressCode,
            CongressName = FirstNonEmpty(detailedSource.CongressName, source.CongressName),
            CongressEnglishName = FirstNonEmpty(
                detailedSource.CongressEnglishName,
                detailedSource.CongressName,
                source.CongressName),
            CongressSubtitle = detailedSource.CongressSubtitle,
            StartDate = detailedSource.StartDate ?? source.StartDate,
            EndDate = detailedSource.EndDate ?? source.EndDate,
            Venue = detailedSource.Venue,
            City = FirstNonEmpty(request.Options.City, detailedSource.City),
            Options = request.Options,
            Boards = request.Options.IncludeBoards
                ? source.BoardSections
                : Array.Empty<ProgramBoardSectionDto>(),
            Entries = entries
        };
    }

    private async Task<IReadOnlyDictionary<Guid, int>> BuildProgramOrderAsync(
        Guid congressId,
        CancellationToken cancellationToken)
    {
        CongressProgramPlan? plan = await _programRepository.GetPlanForDisplayAsync(
            congressId,
            cancellationToken);

        if (plan is null)
            return new Dictionary<Guid, int>();

        Dictionary<Guid, int> result = new();
        int sequence = 0;

        foreach (CongressProgramItem item in plan.Days
                     .OrderBy(day => day.Order <= 0 ? int.MaxValue : day.Order)
                     .ThenBy(day => day.Date)
                     .SelectMany(day => day.Sessions
                         .OrderBy(session => session.StartTime)
                         .ThenBy(session => session.EventRoom.Order <= 0 ? int.MaxValue : session.EventRoom.Order)
                         .ThenBy(session => session.Order <= 0 ? int.MaxValue : session.Order))
                     .SelectMany(session => session.Items
                         .OrderBy(item => item.Order <= 0 ? int.MaxValue : item.Order)))
        {
            if (!result.ContainsKey(item.SubmissionId))
                result[item.SubmissionId] = sequence++;
        }

        return result;
    }

    private static List<AbstractBookEntryDto> ApplySort(
        IEnumerable<AbstractBookEntryDto> entries,
        AbstractBookSortMode sortMode,
        IReadOnlyDictionary<Guid, int> programOrder)
    {
        IOrderedEnumerable<AbstractBookEntryDto> ordered = sortMode switch
        {
            AbstractBookSortMode.Title => entries
                .OrderBy(x => FirstNonEmpty(x.TurkishTitle, x.EnglishTitle), StringComparer.CurrentCultureIgnoreCase),
            AbstractBookSortMode.Topic => entries
                .OrderBy(x => x.TopicName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.SubmissionNumber, StringComparer.OrdinalIgnoreCase),
            AbstractBookSortMode.SubmissionType => entries
                .OrderBy(x => x.SubmissionTypeName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(x => x.SubmissionNumber, StringComparer.OrdinalIgnoreCase),
            AbstractBookSortMode.ProgramOrder => entries
                .OrderBy(x => programOrder.TryGetValue(x.Id, out int order) ? order : int.MaxValue)
                .ThenBy(x => x.SubmissionNumber, StringComparer.OrdinalIgnoreCase),
            _ => entries.OrderBy(x => x.SubmissionNumber, StringComparer.OrdinalIgnoreCase)
        };

        return ordered.ThenBy(x => x.Id).ToList();
    }


    private async Task EnsureHeaderLogoAsync(
        Guid congressId,
        string? culture,
        AbstractBookOptionsDto options,
        CancellationToken cancellationToken)
    {
        if (options.HeaderLogoBytes is { Length: > 0 })
            return;

        string bucketName = _storageOptions.Buckets.CongressImages?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(bucketName))
            return;

        string? logoPath = await _abstractBookRepository.GetCongressLogoUrlAsync(
            congressId,
            culture,
            cancellationToken);

        if (!TryResolveObjectName(logoPath, bucketName, out string objectName))
            return;

        try
        {
            await using Stream source = await _objectStorageService.OpenReadAsync(
                bucketName,
                objectName,
                cancellationToken);
            using MemoryStream buffer = new();
            await source.CopyToAsync(buffer, cancellationToken);

            if (buffer.Length <= 0 || buffer.Length > MaxHeaderLogoSizeInBytes)
            {
                _logger.LogWarning(
                    "Abstract book header logo ignored because its size is invalid. CongressId: {CongressId}, ObjectName: {ObjectName}, Size: {Size}",
                    congressId,
                    objectName,
                    buffer.Length);
                return;
            }

            byte[] bytes = buffer.ToArray();
            string? contentType = DetectSupportedImageContentType(bytes);
            if (contentType is null)
            {
                _logger.LogWarning(
                    "Abstract book header logo ignored because its format is unsupported. CongressId: {CongressId}, ObjectName: {ObjectName}",
                    congressId,
                    objectName);
                return;
            }

            options.HeaderLogoBytes = bytes;
            options.HeaderLogoContentType = contentType;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // A missing/legacy logo must not block the entire abstract book export.
            _logger.LogWarning(
                exception,
                "Congress logo could not be loaded for abstract book header. CongressId: {CongressId}, ObjectName: {ObjectName}",
                congressId,
                objectName);
        }
    }

    private static bool TryResolveObjectName(
        string? value,
        string bucketName,
        out string objectName)
    {
        objectName = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string normalized;
        bool isAbsoluteUrl;
        try
        {
            normalized = value.Trim().Replace('\\', '/');
            isAbsoluteUrl = Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri);
            normalized = isAbsoluteUrl
                ? Uri.UnescapeDataString(uri!.AbsolutePath).Trim('/')
                : Uri.UnescapeDataString(normalized).Trim();
        }
        catch (UriFormatException)
        {
            return false;
        }

        normalized = normalized.TrimStart('~').Trim('/');
        string normalizedBucket = bucketName.Trim().Trim('/');
        string publicPrefix = $"public-assets/{normalizedBucket}/";
        string bucketPrefix = normalizedBucket + "/";

        if (normalized.StartsWith(publicPrefix, StringComparison.OrdinalIgnoreCase))
            normalized = normalized[publicPrefix.Length..];
        else if (normalized.StartsWith(bucketPrefix, StringComparison.OrdinalIgnoreCase))
            normalized = normalized[bucketPrefix.Length..];
        else if (isAbsoluteUrl || normalized.StartsWith("public-assets/", StringComparison.OrdinalIgnoreCase))
            return false;

        normalized = normalized.Trim('/');
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Contains("..", StringComparison.Ordinal))
            return false;

        objectName = normalized;
        return true;
    }

    private static string? DetectSupportedImageContentType(byte[] bytes)
    {
        if (bytes.Length >= 8
            && bytes[0] == 0x89
            && bytes[1] == 0x50
            && bytes[2] == 0x4E
            && bytes[3] == 0x47
            && bytes[4] == 0x0D
            && bytes[5] == 0x0A
            && bytes[6] == 0x1A
            && bytes[7] == 0x0A)
        {
            return "image/png";
        }

        if (bytes.Length >= 3
            && bytes[0] == 0xFF
            && bytes[1] == 0xD8
            && bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        return null;
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? string.Empty;
}
