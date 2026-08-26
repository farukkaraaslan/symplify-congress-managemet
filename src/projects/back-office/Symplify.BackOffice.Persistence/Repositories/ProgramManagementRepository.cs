using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.ProgramManagement.Models;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Persistence.Contexts;

namespace Symplify.BackOffice.Persistence.Repositories;

public sealed class ProgramManagementRepository : IProgramManagementRepository
{
    private static readonly string[] EligibleStatusCodes = { "ACCEPTED", "PAYMENT_PENDING", "COMPLETED" };
    private readonly BackOfficeDbContext _context;

    public ProgramManagementRepository(BackOfficeDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ProgramCongressOptionDto>> GetCongressOptionsAsync(
        string? culture,
        CancellationToken cancellationToken)
    {
        string normalizedCulture = NormalizeCulture(culture);
        Guid? requestedLanguageId = await _context.Languages
            .AsNoTracking()
            .Where(x => x.DeletedDate == null && x.IsActive && x.Culture == normalizedCulture)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        Guid? defaultLanguageId = await _context.Languages
            .AsNoTracking()
            .Where(x => x.DeletedDate == null && x.IsActive && x.IsDefault)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var congresses = await _context.Congresses
            .AsNoTracking()
            .Where(x => x.DeletedDate == null && x.Status == CongressStatus.Published)
            .Include(x => x.Translations)
            .OrderByDescending(x => x.StartDate)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return congresses.Select(x => new ProgramCongressOptionDto(
            x.Id,
            ResolveCongressName(x, requestedLanguageId, defaultLanguageId),
            x.StartDate,
            x.EndDate)).ToList();
    }

    public async Task<ProgramGenerationSourceDto?> GetGenerationSourceAsync(
        Guid congressId,
        IReadOnlyCollection<Guid>? roomIds,
        string? culture,
        CancellationToken cancellationToken,
        ProgramSubmissionFilterDto? filter = null)
    {
        string normalizedCulture = NormalizeCulture(culture);
        Guid? requestedLanguageId = await _context.Languages
            .AsNoTracking()
            .Where(x => x.DeletedDate == null && x.IsActive && x.Culture == normalizedCulture)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        Guid? defaultLanguageId = await _context.Languages
            .AsNoTracking()
            .Where(x => x.DeletedDate == null && x.IsActive && x.IsDefault)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        Congress? congress = await _context.Congresses
            .AsNoTracking()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == congressId
                                      && x.DeletedDate == null
                                      && x.Status == CongressStatus.Published, cancellationToken);

        if (congress is null)
            return null;

        HashSet<Guid> selectedRoomIds = roomIds is null
            ? new HashSet<Guid>()
            : roomIds.Where(x => x != Guid.Empty).ToHashSet();

        var rooms = await _context.EventRooms
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => x.DeletedDate == null && x.IsActive && (selectedRoomIds.Count == 0 || selectedRoomIds.Contains(x.Id)))
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);

        var titles = await _context.Titles
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => x.DeletedDate == null && x.IsActive)
            .OrderBy(x => x.Order <= 0 ? int.MaxValue : x.Order)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);

        IReadOnlyDictionary<string, int> titleOrderLookup = BuildTitleOrderLookup(titles);
        IReadOnlyDictionary<string, string> titleDisplayLookup = BuildTitleDisplayLookup(
            titles,
            requestedLanguageId,
            defaultLanguageId);

        var transactionStatuses = await _context.TransactionStatuses
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var transactionStatusTranslations = await _context.TransactionStatusTranslations
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var paymentStatuses = await _context.PaymentStatuses
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var paymentStatusTranslations = await _context.PaymentStatusTranslations
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var submissionTypes = await _context.SubmissionTypes
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var submissionTypeTranslations = await _context.SubmissionTypeTranslations
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        Dictionary<int, object> paymentStatusById = paymentStatuses
            .Cast<object>()
            .Where(IsLookupActive)
            .Select(x => new { Item = x, Id = ReadNullableIntPropertyValue(x, "Id") })
            .Where(x => x.Id.HasValue)
            .GroupBy(x => x.Id!.Value)
            .ToDictionary(x => x.Key, x => x.First().Item);

        Dictionary<Guid, object> submissionTypeById = submissionTypes
            .Cast<object>()
            .Where(IsLookupActive)
            .Select(x => new { Item = x, Id = ReadNullableGuidPropertyValue(x, "Id") })
            .Where(x => x.Id.HasValue)
            .GroupBy(x => x.Id!.Value)
            .ToDictionary(x => x.Key, x => x.First().Item);

        var allSubmissions = await _context.Submissions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.TransactionStatus)
            .Include(x => x.Topic)
                .ThenInclude(x => x!.Translations)
            .Include(x => x.Authors)
                .ThenInclude(x => x.Title)
                    .ThenInclude(x => x!.Translations)
            .Where(x => x.DeletedDate == null && x.CongressId == congressId)
            .OrderBy(x => x.TopicId)
            .ThenBy(x => x.SubmissionNumber)
            .ToListAsync(cancellationToken);

        List<ProgramSubmissionSourceDto> allSubmissionDtos = allSubmissions
            .Select(submission =>
            {
                string workflowStatusCode = submission.TransactionStatus?.Code?.Trim() ?? string.Empty;
                string workflowStatusName = ResolveLookupDisplayName(
                    submission.TransactionStatus,
                    transactionStatusTranslations.Cast<object>(),
                    "TransactionStatusId",
                    requestedLanguageId,
                    defaultLanguageId,
                    workflowStatusCode);

                int? paymentStatusId = ReadNullableIntPropertyValue(submission, "PaymentStatusId");
                object? paymentStatus = paymentStatusId.HasValue
                    && paymentStatusById.TryGetValue(paymentStatusId.Value, out object? paymentLookup)
                        ? paymentLookup
                        : null;
                string paymentStatusCode = GetLookupCode(paymentStatus);
                string paymentStatusName = ResolveLookupDisplayName(
                    paymentStatus,
                    paymentStatusTranslations.Cast<object>(),
                    "PaymentStatusId",
                    requestedLanguageId,
                    defaultLanguageId,
                    paymentStatusCode);

                Guid? submissionTypeId = ReadNullableGuidPropertyValue(submission, "SubmissionTypeId");
                object? submissionType = submissionTypeId.HasValue
                    && submissionTypeById.TryGetValue(submissionTypeId.Value, out object? submissionTypeLookup)
                        ? submissionTypeLookup
                        : null;
                string submissionTypeCode = GetLookupCode(submissionType);
                string submissionTypeName = ResolveLookupDisplayName(
                    submissionType,
                    submissionTypeTranslations.Cast<object>(),
                    "SubmissionTypeId",
                    requestedLanguageId,
                    defaultLanguageId,
                    submissionTypeCode);

                return new ProgramSubmissionSourceDto(
                    submission.Id,
                    submission.SubmissionNumber,
                    submission.Title,
                    submission.TopicId,
                    ResolveTopicName(submission.Topic, requestedLanguageId, defaultLanguageId),
                    submissionTypeId,
                    submissionTypeName,
                    workflowStatusCode,
                    workflowStatusName,
                    paymentStatusId,
                    paymentStatusCode,
                    paymentStatusName,
                    IsAcceptedStatus(workflowStatusCode),
                    IsPaidPaymentStatus(paymentStatusCode),
                    BuildAuthors(submission.Authors, requestedLanguageId, defaultLanguageId),
                    submission.Authors.Select(BuildAuthorKey).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
            })
            .ToList();

        IReadOnlyList<ProgramSubmissionSourceDto> filterMatchedSubmissions = ApplySubmissionFilter(
            allSubmissionDtos,
            filter);

        // EligibleSubmissionIds is a snapshot of timed programme candidates. Older
        // plans were created before video submissions were added to that snapshot.
        // Apply the saved business filters without the snapshot restriction when
        // resolving video presentations so existing plans can still show them.
        ProgramSubmissionFilterDto? videoFilter = filter is null
            ? null
            : new ProgramSubmissionFilterDto
            {
                Preset = filter.Preset,
                WorkflowStatusCodes = filter.WorkflowStatusCodes,
                PaymentStatusIds = filter.PaymentStatusIds,
                SubmissionTypeIds = filter.SubmissionTypeIds,
                TopicIds = filter.TopicIds,
                SearchText = filter.SearchText
            };

        IReadOnlyList<ProgramSubmissionSourceDto> videoFilterMatchedSubmissions = ApplySubmissionFilter(
            allSubmissionDtos,
            videoFilter);

        var videoFileCandidates = await _context.SubmissionFiles
            .AsNoTracking()
            .Where(file =>
                file.DeletedDate == null &&
                file.IsActive &&
                file.FileKind == SubmissionFileKind.Presentation &&
                file.Submission.DeletedDate == null &&
                file.Submission.CongressId == congressId)
            .Select(file => new
            {
                file.Id,
                file.SubmissionId,
                file.VersionNo,
                file.CreatedDate,
                file.ReviewStatus,
                file.IsIncludedInProgramBook
            })
            .ToListAsync(cancellationToken);

        var latestVideoFiles = videoFileCandidates
            .GroupBy(file => file.SubmissionId)
            .Select(group => group
                .OrderByDescending(file => file.VersionNo)
                .ThenByDescending(file => file.CreatedDate)
                .ThenByDescending(file => file.Id)
                .First())
            .ToList();

        Guid[] publicVideoFileIds = latestVideoFiles
            .Where(file =>
                file.ReviewStatus == SubmissionFileReviewStatus.Approved &&
                file.IsIncludedInProgramBook)
            .Select(file => file.Id)
            .ToArray();
        DateTime now = DateTime.UtcNow;

        var activeVideoShortLinks = publicVideoFileIds.Length == 0
            ? new List<Symplify.BackOffice.Domain.ShortLinks.ShortLink>()
            : await _context.ShortLinks
                .AsNoTracking()
                .Where(link =>
                    link.DeletedDate == null &&
                    link.IsActive &&
                    link.TargetType == ShortLinkTargetType.SubmissionPresentationVideo &&
                    publicVideoFileIds.Contains(link.TargetId) &&
                    (!link.ExpiresAt.HasValue || link.ExpiresAt.Value > now))
                .OrderByDescending(link => link.CreatedDate)
                .ToListAsync(cancellationToken);

        Dictionary<Guid, string> shortLinkCodeByFileId = activeVideoShortLinks
            .GroupBy(link => link.TargetId)
            .ToDictionary(
                group => group.Key,
                group => group.First().Code);

        Dictionary<Guid, ProgramSubmissionSourceDto> submissionDtoById = allSubmissionDtos
            .ToDictionary(submission => submission.Id);

        HashSet<Guid> videoFilterMatchedIds = videoFilterMatchedSubmissions
            .Select(submission => submission.Id)
            .ToHashSet();

        List<ProgramVideoPresentationDto> videoPresentations = latestVideoFiles
            .Where(file =>
                videoFilterMatchedIds.Contains(file.SubmissionId) &&
                submissionDtoById.ContainsKey(file.SubmissionId))
            .Select(file =>
            {
                ProgramSubmissionSourceDto submission = submissionDtoById[file.SubmissionId];
                shortLinkCodeByFileId.TryGetValue(file.Id, out string? shortLinkCode);
                return new ProgramVideoPresentationDto(
                    submission.Id,
                    submission.SubmissionNumber,
                    submission.Title,
                    submission.Authors,
                    shortLinkCode);
            })
            .OrderBy(item => item.SubmissionNumber)
            .ThenBy(item => item.Title)
            .ToList();

        HashSet<Guid> videoSubmissionIds = videoPresentations
            .Select(item => item.SubmissionId)
            .ToHashSet();

        IReadOnlyList<ProgramSubmissionSourceDto> filteredSubmissions = filterMatchedSubmissions
            .Where(submission => !videoSubmissionIds.Contains(submission.Id))
            .ToList();

        var boardMembers = await _context.CongressBoardMembers
            .AsNoTracking()
            .Include(x => x.Translations)
            .Include(x => x.CongressBoard)
            .Where(x => x.DeletedDate == null
                        && x.IsActive
                        && x.CongressBoard.DeletedDate == null
                        && x.CongressBoard.IsActive
                        && x.CongressBoard.CongressId == congressId)
            .OrderBy(x => x.CongressBoard.Order)
            .ThenBy(x => x.Order)
            .ThenBy(x => x.FullName)
            .ToListAsync(cancellationToken);

        var boardTranslations = await _context.CongressBoardTranslations
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        ProgramSubmissionFilterOptionsDto filterOptions = new()
        {
            WorkflowStatuses = allSubmissionDtos
                .Where(x => !string.IsNullOrWhiteSpace(x.WorkflowStatusCode))
                .GroupBy(x => x.WorkflowStatusCode, StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    object? root = transactionStatuses
                        .Cast<object>()
                        .FirstOrDefault(status => string.Equals(
                            GetLookupCode(status),
                            group.Key,
                            StringComparison.OrdinalIgnoreCase));
                    return new ProgramStringFilterOptionDto(
                        group.Key,
                        group.Select(x => x.WorkflowStatusName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? group.Key,
                        ReadLookupOrder(root));
                })
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Name)
                .ToList(),
            PaymentStatuses = paymentStatusById
                .Select(pair => new ProgramIntFilterOptionDto(
                    pair.Key,
                    GetLookupCode(pair.Value),
                    ResolveLookupDisplayName(
                        pair.Value,
                        paymentStatusTranslations.Cast<object>(),
                        "PaymentStatusId",
                        requestedLanguageId,
                        defaultLanguageId,
                        GetLookupCode(pair.Value)),
                    ReadLookupOrder(pair.Value)))
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Name)
                .ToList(),
            SubmissionTypes = submissionTypeById
                .Select(pair => new ProgramGuidFilterOptionDto(
                    pair.Key,
                    GetLookupCode(pair.Value),
                    ResolveLookupDisplayName(
                        pair.Value,
                        submissionTypeTranslations.Cast<object>(),
                        "SubmissionTypeId",
                        requestedLanguageId,
                        defaultLanguageId,
                        GetLookupCode(pair.Value)),
                    ReadLookupOrder(pair.Value)))
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Name)
                .ToList(),
            Topics = allSubmissionDtos
                .Where(x => x.TopicId.HasValue)
                .GroupBy(x => x.TopicId!.Value)
                .Select(group => new ProgramGuidFilterOptionDto(
                    group.Key,
                    string.Empty,
                    group.Select(x => x.TopicName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? group.Key.ToString(),
                    int.MaxValue))
                .OrderBy(x => x.Name)
                .ToList()
        };

        return new ProgramGenerationSourceDto
        {
            CongressId = congress.Id,
            CongressName = ResolveCongressName(congress, requestedLanguageId, defaultLanguageId),
            StartDate = congress.StartDate,
            EndDate = congress.EndDate,
            Rooms = rooms.Select(x => new ProgramRoomOptionDto(
                x.Id,
                ResolveRoomName(x, requestedLanguageId, defaultLanguageId),
                x.Order)).ToList(),
            AuthorOptions = allSubmissions
                .SelectMany(x => x.Authors)
                .GroupBy(x => x.Id)
                .Select(group => group.First())
                .Select(author => new ProgramAuthorOptionDto(
                    author.Id,
                    BuildAuthorDisplayName(author, requestedLanguageId, defaultLanguageId),
                    author.Institution?.Trim() ?? string.Empty,
                    author.Email,
                    NormalizeTitleOrder(author.Title?.Order ?? int.MaxValue),
                    BuildAuthorKey(author)))
                .OrderBy(x => x.TitleOrder)
                .ThenBy(x => x.DisplayName)
                .ThenBy(x => x.Institution)
                .ToList(),
            BoardMemberOptions = boardMembers
                .Select(member => BuildBoardMemberOption(
                    member,
                    requestedLanguageId,
                    defaultLanguageId,
                    titleOrderLookup,
                    titleDisplayLookup))
                .OrderBy(x => x.TitleOrder)
                .ThenBy(x => x.DisplayName)
                .ThenBy(x => x.Institution)
                .ToList(),
            BoardSections = boardMembers
                .GroupBy(member => member.CongressBoard.Id)
                .Select(group =>
                {
                    CongressBoard board = group.First().CongressBoard;
                    return new ProgramBoardSectionDto(
                        board.Id,
                        ResolveBoardName(
                            board,
                            boardTranslations.Cast<object>(),
                            requestedLanguageId,
                            defaultLanguageId),
                        board.Order,
                        group
                            .OrderBy(member => member.Order <= 0 ? int.MaxValue : member.Order)
                            .ThenBy(member => member.FullName)
                            .Select(member =>
                            {
                                ProgramBoardMemberOptionDto option = BuildBoardMemberOption(
                                    member,
                                    requestedLanguageId,
                                    defaultLanguageId,
                                    titleOrderLookup,
                                    titleDisplayLookup);
                                return new ProgramBoardMemberPdfDto(
                                    member.Id,
                                    option.DisplayName,
                                    option.Institution,
                                    member.Order);
                            })
                            .ToList());
                })
                .OrderBy(section => section.Order <= 0 ? int.MaxValue : section.Order)
                .ThenBy(section => section.Name)
                .ToList(),
            FilterOptions = filterOptions,
            AllSubmissions = allSubmissionDtos,
            FilteredSubmissions = filterMatchedSubmissions,
            Submissions = filteredSubmissions,
            VideoPresentations = videoPresentations
        };
    }

    public Task<CongressProgramPlan?> GetPlanForDisplayAsync(Guid congressId, CancellationToken cancellationToken)
    {
        return _context.CongressProgramPlans
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Days)
                .ThenInclude(x => x.FixedBlocks)
            .Include(x => x.Days)
                .ThenInclude(x => x.Sessions)
                    .ThenInclude(x => x.EventRoom)
                        .ThenInclude(x => x.Translations)
            .Include(x => x.Days)
                .ThenInclude(x => x.Sessions)
                    .ThenInclude(x => x.Items)
                        .ThenInclude(x => x.Submission)
                            .ThenInclude(x => x.Authors)
            .Include(x => x.Days)
                .ThenInclude(x => x.Sessions)
                    .ThenInclude(x => x.Items)
                        .ThenInclude(x => x.Submission)
                            .ThenInclude(x => x.Topic)
                                .ThenInclude(x => x!.Translations)
            .FirstOrDefaultAsync(x => x.CongressId == congressId
                                      && x.DeletedDate == null
                                      && x.Congress.Status == CongressStatus.Published, cancellationToken);
    }

    public Task<CongressProgramPlan?> GetPlanForUpdateAsync(Guid congressId, CancellationToken cancellationToken)
    {
        return _context.CongressProgramPlans
            .AsSplitQuery()
            .Include(x => x.Days)
                .ThenInclude(x => x.FixedBlocks)
            .Include(x => x.Days)
                .ThenInclude(x => x.Sessions)
                    .ThenInclude(x => x.Items)
                        .ThenInclude(x => x.Submission)
                            .ThenInclude(x => x.Authors)
            .FirstOrDefaultAsync(x => x.CongressId == congressId
                                      && x.DeletedDate == null
                                      && x.Congress.Status == CongressStatus.Published, cancellationToken);
    }


    public async Task<bool> AreAuthorsEligibleForCongressAsync(
        Guid congressId,
        IReadOnlyCollection<Guid> authorIds,
        CancellationToken cancellationToken)
    {
        Guid[] normalizedIds = authorIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalizedIds.Length == 0)
            return true;

        int matchedCount = await _context.Authors
            .AsNoTracking()
            .Where(author => normalizedIds.Contains(author.Id)
                             && author.DeletedDate == null
                             && author.Submissions.Any(submission =>
                                 submission.DeletedDate == null
                                 && submission.CongressId == congressId))
            .Select(author => author.Id)
            .Distinct()
            .CountAsync(cancellationToken);

        return matchedCount == normalizedIds.Length;
    }

    public async Task<bool> AreBoardMembersEligibleForCongressAsync(
        Guid congressId,
        IReadOnlyCollection<Guid> boardMemberIds,
        CancellationToken cancellationToken)
    {
        Guid[] normalizedIds = boardMemberIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalizedIds.Length == 0)
            return true;

        int matchedCount = await _context.CongressBoardMembers
            .AsNoTracking()
            .Where(member => normalizedIds.Contains(member.Id)
                             && member.DeletedDate == null
                             && member.IsActive
                             && member.CongressBoard.DeletedDate == null
                             && member.CongressBoard.IsActive
                             && member.CongressBoard.CongressId == congressId)
            .Select(member => member.Id)
            .Distinct()
            .CountAsync(cancellationToken);

        return matchedCount == normalizedIds.Length;
    }

    public async Task AddPlanAsync(CongressProgramPlan plan, CancellationToken cancellationToken)
    {
        await _context.CongressProgramPlans.AddAsync(plan, cancellationToken);
    }

    public void RemovePlan(CongressProgramPlan plan)
    {
        _context.CongressProgramPlans.Remove(plan);
    }

    public void RemoveFixedBlock(CongressProgramFixedBlock fixedBlock)
    {
        _context.CongressProgramFixedBlocks.Remove(fixedBlock);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }




    private static IReadOnlyList<ProgramSubmissionSourceDto> ApplySubmissionFilter(
        IReadOnlyList<ProgramSubmissionSourceDto> submissions,
        ProgramSubmissionFilterDto? filter)
    {
        if (filter is null)
            return submissions;

        HashSet<string> workflowCodes = filter.WorkflowStatusCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<int> paymentStatusIds = filter.PaymentStatusIds.ToHashSet();
        HashSet<Guid> submissionTypeIds = filter.SubmissionTypeIds
            .Where(x => x != Guid.Empty)
            .ToHashSet();
        HashSet<Guid> topicIds = filter.TopicIds
            .Where(x => x != Guid.Empty)
            .ToHashSet();
        HashSet<Guid> includedSubmissionIds = filter.IncludedSubmissionIds
            .Where(x => x != Guid.Empty)
            .ToHashSet();
        string searchText = NormalizeFilterText(filter.SearchText);

        IEnumerable<ProgramSubmissionSourceDto> query = submissions;

        if (includedSubmissionIds.Count > 0)
            query = query.Where(x => includedSubmissionIds.Contains(x.Id));

        query = filter.Preset switch
        {
            ProgramSubmissionScopePreset.AcceptedOnly => query.Where(x => x.IsAccepted),
            ProgramSubmissionScopePreset.PaidOnly => query.Where(x => x.IsPaid),
            ProgramSubmissionScopePreset.AcceptedAndPaid => query.Where(x => x.IsAccepted && x.IsPaid),
            _ => query
        };

        if (workflowCodes.Count > 0)
            query = query.Where(x => workflowCodes.Contains(x.WorkflowStatusCode));
        if (paymentStatusIds.Count > 0)
            query = query.Where(x => x.PaymentStatusId.HasValue && paymentStatusIds.Contains(x.PaymentStatusId.Value));
        if (submissionTypeIds.Count > 0)
            query = query.Where(x => x.SubmissionTypeId.HasValue && submissionTypeIds.Contains(x.SubmissionTypeId.Value));
        if (topicIds.Count > 0)
            query = query.Where(x => x.TopicId.HasValue && topicIds.Contains(x.TopicId.Value));

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(x => NormalizeFilterText(
                    $"{x.SubmissionNumber} {x.Title} {x.Authors} {x.TopicName} {x.SubmissionTypeName} {x.WorkflowStatusName} {x.PaymentStatusName}")
                .Contains(searchText, StringComparison.Ordinal));
        }

        return query
            .OrderBy(x => x.TopicName)
            .ThenBy(x => x.SubmissionNumber)
            .ToList();
    }

    private static bool IsAcceptedStatus(string? code)
        => !string.IsNullOrWhiteSpace(code)
           && EligibleStatusCodes.Contains(code.Trim(), StringComparer.OrdinalIgnoreCase);

    private static bool IsPaidPaymentStatus(string? code)
    {
        string normalized = NormalizeLookupCode(code);
        return normalized is "PAYMENTCOMPLETED" or "PAID" or "COMPLETED"
               || normalized.Contains("PAYMENTCOMPLET", StringComparison.Ordinal)
               || normalized.Contains("ODEMEYAPILDI", StringComparison.Ordinal);
    }

    private static string ResolveLookupDisplayName(
        object? root,
        IEnumerable<object> translations,
        string foreignKeyPropertyName,
        Guid? requestedLanguageId,
        Guid? defaultLanguageId,
        string? fallback)
    {
        if (root is null)
            return fallback?.Trim() ?? string.Empty;

        object? rootId = ReadObjectProperty(root, "Id");
        List<object> matchingTranslations = translations
            .Where(x => Equals(ReadObjectProperty(x, foreignKeyPropertyName), rootId))
            .ToList();

        object? selected = matchingTranslations
            .FirstOrDefault(x => requestedLanguageId.HasValue
                                 && ReadNullableGuidPropertyValue(x, "LanguageId") == requestedLanguageId.Value)
            ?? matchingTranslations.FirstOrDefault(x => defaultLanguageId.HasValue
                                                         && ReadNullableGuidPropertyValue(x, "LanguageId") == defaultLanguageId.Value)
            ?? matchingTranslations.FirstOrDefault();

        string translated = ReadFirstNonEmptyString(selected, "Name", "Title", "Description", "DisplayName", "Value");
        if (!string.IsNullOrWhiteSpace(translated))
            return translated;

        string rootName = ReadFirstNonEmptyString(root, "Name", "Title", "Description", "Code");
        return !string.IsNullOrWhiteSpace(rootName)
            ? rootName
            : fallback?.Trim() ?? string.Empty;
    }

    private static bool IsLookupActive(object source)
    {
        object? deletedDate = ReadObjectProperty(source, "DeletedDate");
        if (deletedDate is not null)
            return false;

        object? isActive = ReadObjectProperty(source, "IsActive");
        return isActive is not bool boolValue || boolValue;
    }

    private static string GetLookupCode(object? source)
        => ReadFirstNonEmptyString(source, "Code", "Name");

    private static int ReadLookupOrder(object? source)
    {
        object? value = ReadObjectProperty(source, "Order")
                        ?? ReadObjectProperty(source, "OrderNo")
                        ?? ReadObjectProperty(source, "SortOrder");
        int order = value switch
        {
            int intValue => intValue,
            short shortValue => shortValue,
            long longValue when longValue <= int.MaxValue => (int)longValue,
            _ => int.MaxValue
        };
        return order > 0 ? order : int.MaxValue;
    }

    private static object? ReadObjectProperty(object? source, string propertyName)
        => source?.GetType().GetProperty(propertyName)?.GetValue(source);

    private static int? ReadNullableIntPropertyValue(object? source, string propertyName)
    {
        object? value = ReadObjectProperty(source, propertyName);
        return value switch
        {
            int intValue => intValue,
            short shortValue => shortValue,
            long longValue when longValue <= int.MaxValue => (int)longValue,
            _ => null
        };
    }

    private static Guid? ReadNullableGuidPropertyValue(object? source, string propertyName)
    {
        object? value = ReadObjectProperty(source, propertyName);
        return value is Guid guid && guid != Guid.Empty ? guid : null;
    }

    private static string NormalizeLookupCode(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value
                .Trim()
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());

    private static string NormalizeFilterText(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();

    private static string ResolveBoardName(
        CongressBoard board,
        IEnumerable<object> translations,
        Guid? requestedLanguageId,
        Guid? defaultLanguageId)
    {
        List<object> boardTranslations = translations
            .Where(translation => ReadGuidProperty(translation, "CongressBoardId") == board.Id)
            .ToList();

        object? selected = boardTranslations
            .FirstOrDefault(translation => requestedLanguageId.HasValue
                                           && ReadGuidProperty(translation, "LanguageId") == requestedLanguageId.Value)
            ?? boardTranslations.FirstOrDefault(translation => defaultLanguageId.HasValue
                                                                && ReadGuidProperty(translation, "LanguageId") == defaultLanguageId.Value)
            ?? boardTranslations.FirstOrDefault();

        string translatedName = ReadFirstNonEmptyString(selected, "Title", "Name", "Description");
        if (!string.IsNullOrWhiteSpace(translatedName))
            return translatedName;

        string rootName = ReadFirstNonEmptyString(board, "Title", "Name", "Code");
        if (!string.IsNullOrWhiteSpace(rootName))
            return rootName;

        return board.Order > 0 ? $"Kurul {board.Order}" : "Kongre Kurulu";
    }

    private static Guid? ReadGuidProperty(object? source, string propertyName)
    {
        object? value = source?.GetType().GetProperty(propertyName)?.GetValue(source);
        return value is Guid guid ? guid : null;
    }

    private static string ReadFirstNonEmptyString(object? source, params string[] propertyNames)
    {
        if (source is null)
            return string.Empty;

        foreach (string propertyName in propertyNames)
        {
            object? value = source.GetType().GetProperty(propertyName)?.GetValue(source);
            if (value is string text && !string.IsNullOrWhiteSpace(text))
                return text.Trim();
        }

        return string.Empty;
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim()
           ?? string.Empty;

    private static ProgramBoardMemberOptionDto BuildBoardMemberOption(
        CongressBoardMember member,
        Guid? requestedLanguageId,
        Guid? defaultLanguageId,
        IReadOnlyDictionary<string, int> titleOrderLookup,
        IReadOnlyDictionary<string, string> titleDisplayLookup)
    {
        CongressBoardMemberTranslation? translation = member.Translations
            .FirstOrDefault(x => requestedLanguageId.HasValue && x.LanguageId == requestedLanguageId.Value)
            ?? member.Translations.FirstOrDefault(x => defaultLanguageId.HasValue && x.LanguageId == defaultLanguageId.Value)
            ?? member.Translations.OrderBy(x => x.LanguageId).FirstOrDefault();

        string fullName = translation?.FullName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fullName))
            fullName = member.FullName.Trim();

        string storedTitle = translation?.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(storedTitle))
            storedTitle = member.AcademicTitle?.Trim() ?? string.Empty;

        // CongressBoardMember.AcademicTitle currently stores the lookup display name
        // (for example "Profesör Doktor"). Book outputs must use the short form kept
        // in TitleTranslation.Description (for example "Prof. Dr."). Resolve both the
        // translated member title and the legacy root value through the centralized
        // title lookup, while preserving custom/unmatched titles as a safe fallback.
        string title = ResolveBoardMemberTitleDisplay(
            storedTitle,
            member.AcademicTitle,
            titleDisplayLookup);

        string displayName = string.IsNullOrWhiteSpace(title)
            || fullName.StartsWith(title, StringComparison.OrdinalIgnoreCase)
                ? fullName
                : $"{title} {fullName}".Trim();

        string institution = translation?.Institution?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(institution))
            institution = member.Institution?.Trim() ?? string.Empty;

        int titleOrder = ResolveBoardMemberTitleOrder(storedTitle, member.AcademicTitle, titleOrderLookup);

        return new ProgramBoardMemberOptionDto(member.Id, displayName, institution, titleOrder);
    }

    private static IReadOnlyDictionary<string, string> BuildTitleDisplayLookup(
        IEnumerable<Symplify.BackOffice.Domain.Lookups.Title> titles,
        Guid? requestedLanguageId,
        Guid? defaultLanguageId)
    {
        Dictionary<string, string> lookup = new(StringComparer.OrdinalIgnoreCase);

        foreach (Symplify.BackOffice.Domain.Lookups.Title title in titles)
        {
            Symplify.BackOffice.Domain.Lookups.TitleTranslation? selectedTranslation =
                title.Translations.FirstOrDefault(x => requestedLanguageId.HasValue && x.LanguageId == requestedLanguageId.Value)
                ?? title.Translations.FirstOrDefault(x => defaultLanguageId.HasValue && x.LanguageId == defaultLanguageId.Value)
                ?? title.Translations.OrderBy(x => x.LanguageId).FirstOrDefault();

            string displayValue = FirstNonEmpty(
                selectedTranslation?.Description,
                selectedTranslation?.Name,
                title.Code);

            if (string.IsNullOrWhiteSpace(displayValue))
                continue;

            AddTitleDisplayKey(lookup, title.Code, displayValue);

            foreach (Symplify.BackOffice.Domain.Lookups.TitleTranslation translation in title.Translations)
            {
                AddTitleDisplayKey(lookup, translation.Name, displayValue);
                AddTitleDisplayKey(lookup, translation.Description, displayValue);
            }
        }

        return lookup;
    }

    private static string ResolveBoardMemberTitleDisplay(
        string? translatedTitle,
        string? academicTitle,
        IReadOnlyDictionary<string, string> titleDisplayLookup)
    {
        foreach (string? titleText in new[] { translatedTitle, academicTitle })
        {
            string key = NormalizeTitleLookupKey(titleText);
            if (!string.IsNullOrWhiteSpace(key)
                && titleDisplayLookup.TryGetValue(key, out string? displayValue)
                && !string.IsNullOrWhiteSpace(displayValue))
            {
                return displayValue.Trim();
            }
        }

        return FirstNonEmpty(translatedTitle, academicTitle);
    }

    private static void AddTitleDisplayKey(
        IDictionary<string, string> lookup,
        string? value,
        string displayValue)
    {
        string key = NormalizeTitleLookupKey(value);
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(displayValue))
            return;

        lookup.TryAdd(key, displayValue.Trim());
    }

    private static IReadOnlyDictionary<string, int> BuildTitleOrderLookup(
        IEnumerable<Symplify.BackOffice.Domain.Lookups.Title> titles)
    {
        Dictionary<string, int> lookup = new(StringComparer.OrdinalIgnoreCase);

        foreach (Symplify.BackOffice.Domain.Lookups.Title title in titles)
        {
            int order = NormalizeTitleOrder(title.Order);
            AddTitleOrderKey(lookup, title.Code, order);

            foreach (Symplify.BackOffice.Domain.Lookups.TitleTranslation translation in title.Translations)
            {
                AddTitleOrderKey(lookup, translation.Name, order);
                AddTitleOrderKey(lookup, translation.Description, order);
            }
        }

        return lookup;
    }

    private static int ResolveBoardMemberTitleOrder(
        string? translatedTitle,
        string? academicTitle,
        IReadOnlyDictionary<string, int> titleOrderLookup)
    {
        foreach (string? titleText in new[] { translatedTitle, academicTitle })
        {
            string key = NormalizeTitleLookupKey(titleText);
            if (!string.IsNullOrWhiteSpace(key)
                && titleOrderLookup.TryGetValue(key, out int order))
            {
                return order;
            }
        }

        return int.MaxValue;
    }

    private static void AddTitleOrderKey(
        IDictionary<string, int> lookup,
        string? value,
        int order)
    {
        string key = NormalizeTitleLookupKey(value);
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (!lookup.TryGetValue(key, out int currentOrder) || order < currentOrder)
            lookup[key] = order;
    }

    private static int NormalizeTitleOrder(int order)
        => order > 0 ? order : int.MaxValue;

    private static string NormalizeTitleLookupKey(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value
                .Trim()
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());

    private static string BuildAuthorDisplayName(
        Symplify.BackOffice.Domain.Submission.Author author,
        Guid? requestedLanguageId,
        Guid? defaultLanguageId)
    {
        string title = ResolveAuthorTitle(author.Title, requestedLanguageId, defaultLanguageId);
        string fullName = $"{author.FirstName} {author.LastName}".Trim();
        return string.IsNullOrWhiteSpace(title) ? fullName : $"{title} {fullName}".Trim();
    }

    private static string BuildAuthors(
        IEnumerable<Symplify.BackOffice.Domain.Submission.Author> authors,
        Guid? requestedLanguageId,
        Guid? defaultLanguageId)
    {
        return string.Join(" - ", authors
            .OrderByDescending(x => x.IsCorrespondingAuthor)
            .ThenBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .Select(author =>
            {
                string title = ResolveAuthorTitle(author.Title, requestedLanguageId, defaultLanguageId);
                string fullName = $"{author.FirstName} {author.LastName}".Trim();
                return string.IsNullOrWhiteSpace(title) ? fullName : $"{title} {fullName}".Trim();
            }));
    }

    private static string ResolveAuthorTitle(
        Symplify.BackOffice.Domain.Lookups.Title? title,
        Guid? requestedLanguageId,
        Guid? defaultLanguageId)
    {
        if (title is null)
            return string.Empty;

        Symplify.BackOffice.Domain.Lookups.TitleTranslation? translation =
            title.Translations.FirstOrDefault(x => requestedLanguageId.HasValue && x.LanguageId == requestedLanguageId.Value)
            ?? title.Translations.FirstOrDefault(x => defaultLanguageId.HasValue && x.LanguageId == defaultLanguageId.Value)
            ?? title.Translations.OrderBy(x => x.LanguageId).FirstOrDefault();

        // TitleTranslation.Description contains the short form in the existing seed
        // (for example Prof. Dr., Doç. Dr., Dr. Öğr. Üyesi).
        if (!string.IsNullOrWhiteSpace(translation?.Description))
            return translation.Description.Trim();
        if (!string.IsNullOrWhiteSpace(translation?.Name))
            return translation.Name.Trim();
        return title.Code?.Trim() ?? string.Empty;
    }

    private static string BuildAuthorKey(Symplify.BackOffice.Domain.Submission.Author author)
    {
        if (!string.IsNullOrWhiteSpace(author.Orcid))
            return $"orcid:{NormalizeIdentityPart(author.Orcid)}";
        if (!string.IsNullOrWhiteSpace(author.Email))
            return $"email:{NormalizeIdentityPart(author.Email)}";

        return $"name:{NormalizeIdentityPart(author.FirstName)}|{NormalizeIdentityPart(author.LastName)}|{NormalizeIdentityPart(author.Institution)}";
    }

    private static string NormalizeIdentityPart(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();

    private static string ResolveCongressName(Congress congress, Guid? requestedLanguageId, Guid? defaultLanguageId)
    {
        return congress.Translations.FirstOrDefault(x => requestedLanguageId.HasValue && x.LanguageId == requestedLanguageId.Value)?.Title
               ?? congress.Translations.FirstOrDefault(x => defaultLanguageId.HasValue && x.LanguageId == defaultLanguageId.Value)?.Title
               ?? congress.Name;
    }

    private static string ResolveRoomName(Symplify.BackOffice.Domain.Lookups.EventRoom room, Guid? requestedLanguageId, Guid? defaultLanguageId)
    {
        return room.Translations.FirstOrDefault(x => requestedLanguageId.HasValue && x.LanguageId == requestedLanguageId.Value)?.Name
               ?? room.Translations.FirstOrDefault(x => defaultLanguageId.HasValue && x.LanguageId == defaultLanguageId.Value)?.Name
               ?? room.Code
               ?? room.Id.ToString("N")[..8];
    }

    private static string ResolveTopicName(Symplify.BackOffice.Domain.Lookups.Topic? topic, Guid? requestedLanguageId, Guid? defaultLanguageId)
    {
        if (topic is null)
            return "Konu belirtilmemiş";

        return topic.Translations.FirstOrDefault(x => requestedLanguageId.HasValue && x.LanguageId == requestedLanguageId.Value)?.Name
               ?? topic.Translations.FirstOrDefault(x => defaultLanguageId.HasValue && x.LanguageId == defaultLanguageId.Value)?.Name
               ?? "Konu belirtilmemiş";
    }

    private static string NormalizeCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return "tr-TR";
        if (string.Equals(culture, "tr", StringComparison.OrdinalIgnoreCase))
            return "tr-TR";
        if (string.Equals(culture, "en", StringComparison.OrdinalIgnoreCase))
            return "en-US";
        return culture;
    }
}
