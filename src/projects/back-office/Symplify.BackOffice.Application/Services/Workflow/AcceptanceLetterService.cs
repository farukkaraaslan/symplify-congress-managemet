using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Core.Application.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QRCoder;
using Symplify.BackOffice.Application.Common.Storage;
using Symplify.BackOffice.Application.Services.Mailing;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.Urls;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Services.Workflow;

public sealed class AcceptanceLetterService : IAcceptanceLetterService
{
    private static readonly Regex InvalidCharactersRegex = new("[^a-z0-9._-]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex MultipleDashRegex = new("-{2,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ISubmissionRepository _submissionRepository;
    private readonly ISubmissionAcceptanceLetterRepository _acceptanceLetterRepository;
    private readonly ISubmissionFileRepository _submissionFileRepository;
    private readonly ICongressBoardRepository _congressBoardRepository;
    private readonly ICongressBoardTranslationRepository _congressBoardTranslationRepository;
    private readonly ICongressBoardMemberRepository _congressBoardMemberRepository;
    private readonly ITitleTranslationRepository _titleTranslationRepository;
    private readonly IStateRepository _stateRepository;
    private readonly IStateTranslationRepository _stateTranslationRepository;
    private readonly ICountryTranslationRepository _countryTranslationRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ObjectStorageOptions _storageOptions;
    private readonly IPublicUrlService _publicUrlService;
    private readonly IAcceptanceLetterPdfRenderer _pdfRenderer;

    public AcceptanceLetterService(
        ISubmissionRepository submissionRepository,
        ISubmissionAcceptanceLetterRepository acceptanceLetterRepository,
        ISubmissionFileRepository submissionFileRepository,
        ICongressBoardRepository congressBoardRepository,
        ICongressBoardTranslationRepository congressBoardTranslationRepository,
        ICongressBoardMemberRepository congressBoardMemberRepository,
        ITitleTranslationRepository titleTranslationRepository,
        IStateRepository stateRepository,
        IStateTranslationRepository stateTranslationRepository,
        ICountryTranslationRepository countryTranslationRepository,
        IObjectStorageService objectStorageService,
        IOptions<ObjectStorageOptions> storageOptions,
        IPublicUrlService publicUrlService,
        IAcceptanceLetterPdfRenderer pdfRenderer)
    {
        _submissionRepository = submissionRepository;
        _acceptanceLetterRepository = acceptanceLetterRepository;
        _submissionFileRepository = submissionFileRepository;
        _congressBoardRepository = congressBoardRepository;
        _congressBoardTranslationRepository = congressBoardTranslationRepository;
        _congressBoardMemberRepository = congressBoardMemberRepository;
        _titleTranslationRepository = titleTranslationRepository;
        _stateRepository = stateRepository;
        _stateTranslationRepository = stateTranslationRepository;
        _countryTranslationRepository = countryTranslationRepository;
        _objectStorageService = objectStorageService;
        _storageOptions = storageOptions.Value;
        _publicUrlService = publicUrlService;
        _pdfRenderer = pdfRenderer;
    }

    public async Task<IReadOnlyList<SubmissionAcceptanceLetter>> GenerateAsync(Submission submission, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);

        Submission aggregate = await LoadSubmissionAggregateAsync(submission.Id, cancellationToken);
        List<Author> authors = GetActiveAuthors(aggregate);

        if (authors.Count == 0)
            return Array.Empty<SubmissionAcceptanceLetter>();

        AcceptanceLetterResources resources = await ResolveResourcesAsync(aggregate, cancellationToken);
        DateTime now = DateTime.UtcNow;
        const string auditActor = "AcceptanceLetterGenerated";

        List<SubmissionAcceptanceLetter> result = new();

        foreach (Author author in authors)
        {
            SubmissionAcceptanceLetter? existing = await GetCurrentLetterAsync(
                aggregate.Id,
                author.Id,
                aggregate.LanguageId,
                cancellationToken);

            if (existing is not null)
            {
                await AddSubmissionFileRecordAsync(aggregate.Id, existing, existing.GeneratedAt, auditActor, cancellationToken);
                result.Add(existing);
                continue;
            }

            SubmissionAcceptanceLetter letter = await CreateOrUpdateAuthorLetterAsync(
                aggregate,
                author,
                resources,
                existing: null,
                now: now,
                auditActor: auditActor,
                resetSentInfo: false,
                cancellationToken);

            result.Add(letter);
        }

        return result;
    }

    public async Task<IReadOnlyList<SubmissionAcceptanceLetter>> ReplaceCurrentAsync(
        Submission submission,
        Guid? performedByUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);

        Submission aggregate = await LoadSubmissionAggregateAsync(submission.Id, cancellationToken);
        List<Author> authors = GetActiveAuthors(aggregate);

        if (authors.Count == 0)
            return Array.Empty<SubmissionAcceptanceLetter>();

        AcceptanceLetterResources resources = await ResolveResourcesAsync(aggregate, cancellationToken);
        DateTime now = DateTime.UtcNow;
        string auditActor = performedByUserId?.ToString("D") ?? "AcceptanceLetterRegenerated";

        List<StorageObjectReference> previousObjects = await GetExistingAcceptanceLetterObjectReferencesAsync(
            aggregate.Id,
            cancellationToken);

        List<SubmissionAcceptanceLetter> result = new();
        HashSet<string> currentObjectKeys = new(StringComparer.Ordinal);
        HashSet<string> currentFilePaths = new(StringComparer.Ordinal);

        foreach (Author author in authors)
        {
            SubmissionAcceptanceLetter? existing = await GetCurrentLetterAsync(
                aggregate.Id,
                author.Id,
                aggregate.LanguageId,
                cancellationToken);

            AddObjectReference(previousObjects, existing?.PdfBucketName, existing?.PdfObjectName);
            AddObjectReference(previousObjects, existing?.PdfBucketName, existing?.PdfFilePath);

            SubmissionAcceptanceLetter letter = await CreateOrUpdateAuthorLetterAsync(
                aggregate,
                author,
                resources,
                existing: existing,
                now: now,
                auditActor: auditActor,
                resetSentInfo: true,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(letter.PdfBucketName) && !string.IsNullOrWhiteSpace(letter.PdfObjectName))
                currentObjectKeys.Add(BuildObjectReferenceKey(letter.PdfBucketName, letter.PdfObjectName));

            string? currentFilePath = letter.PdfObjectName ?? letter.PdfFilePath;
            if (!string.IsNullOrWhiteSpace(currentFilePath))
                currentFilePaths.Add(currentFilePath.Trim());

            result.Add(letter);
        }

        await DeactivateObsoleteAcceptanceLetterFileRecordsAsync(
            aggregate.Id,
            currentFilePaths,
            now,
            auditActor,
            cancellationToken);

        await DeletePreviousObjectsAsync(previousObjects, currentObjectKeys, cancellationToken);

        return result;
    }

    public async Task<bool> HasMissingCurrentLettersAsync(
        Submission submission,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);

        Submission aggregate = await LoadSubmissionAggregateAsync(submission.Id, cancellationToken);
        List<Guid> activeAuthorIds = GetActiveAuthors(aggregate)
            .Select(author => author.Id)
            .Distinct()
            .ToList();

        if (activeAuthorIds.Count == 0)
            return false;

        List<Guid> existingLetterAuthorIds = await _acceptanceLetterRepository
            .Query()
            .AsNoTracking()
            .Where(letter =>
                letter.SubmissionId == aggregate.Id &&
                letter.LanguageId == aggregate.LanguageId &&
                letter.AuthorId.HasValue &&
                activeAuthorIds.Contains(letter.AuthorId.Value) &&
                letter.DeletedDate == null &&
                ((letter.PdfObjectName != null && letter.PdfObjectName != string.Empty) ||
                 (letter.PdfFilePath != null && letter.PdfFilePath != string.Empty)))
            .Select(letter => letter.AuthorId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        return activeAuthorIds.Any(authorId => !existingLetterAuthorIds.Contains(authorId));
    }

    private async Task<Submission> LoadSubmissionAggregateAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        Submission? aggregate = await _submissionRepository
            .Query()
            .Include(item => item.Authors)
                .ThenInclude(author => author.Title)
                    .ThenInclude(title => title!.Translations)
            .Include(item => item.Language)
            .Include(item => item.Congress)
                .ThenInclude(congress => congress.Organization)
            .Include(item => item.Congress)
                .ThenInclude(congress => congress.Translations)
                    .ThenInclude(translation => translation.Language)
            .Include(item => item.Congress)
                .ThenInclude(congress => congress.ContactEmails)
            .Include(item => item.SubmissionType)
                .ThenInclude(type => type!.Translations)
                    .ThenInclude(translation => translation.Language)
            .FirstOrDefaultAsync(item => item.Id == submissionId, cancellationToken);

        if (aggregate is null)
            throw new InvalidOperationException("Submission not found.");

        return aggregate;
    }

    private async Task<SubmissionAcceptanceLetter> CreateOrUpdateAuthorLetterAsync(
        Submission submission,
        Author author,
        AcceptanceLetterResources resources,
        SubmissionAcceptanceLetter? existing,
        DateTime now,
        string auditActor,
        bool resetSentInfo,
        CancellationToken cancellationToken)
    {
        string submissionNumber = ResolveSubmissionNumber(submission);
        string authorFullName = ResolveAuthorDisplayName(author, submission.LanguageId);
        string safeAuthorSegment = Slug(NormalizePersonName(author));
        string fileName = $"{Slug(submissionNumber)}_{safeAuthorSegment}_acceptance-letter.pdf";
        string bucketName = GetSubmissionsBucketName();
        string objectName = BuildObjectName(submissionNumber, author.Id, fileName);
        string organizationShortName = ResolveOrganizationShortName(submission.Congress);
        string congressTitle = ResolveEnglishCongressTitle(submission);
        string congressLocation = await ResolveCongressLocationAsync(
            submission.Congress,
            cancellationToken);
        string congressDateRange = ResolveEnglishCongressDateRange(submission.Congress);
        string submissionTypeName = ResolveEnglishSubmissionTypeName(submission);
        string signerAcademicTitle = await ResolveSignerAcademicTitleAsync(resources.Signer.Member.AcademicTitle, submission.LanguageId, cancellationToken);
        string signerName = ResolveSignerDisplayName(resources.Signer.Member, submission.Congress, signerAcademicTitle);
        string signerDuty = ResolveSignerDuty(resources.Signer, submission.Congress);
        string organizationName = ResolveOrganizationName(submission.Congress);
        string organizationEmail = ResolveCongressEmails(submission.Congress);
        string bodyContent = BuildBodyContent(
            authorFullName,
            submission.Title,
            submissionTypeName,
            congressTitle,
            congressDateRange,
            organizationShortName);
        string verificationCode = BuildVerificationCode(organizationShortName, submissionNumber, author.Id);
        string verificationUrl = BuildVerificationUrl(verificationCode);
        byte[]? qrCodeBytes = TryGenerateQrCodeBytes(verificationUrl);

        AcceptanceLetterPdfModel pdfModel = new()
        {
            OrganizationShortName = organizationShortName,
            OrganizationName = organizationName,
            OrganizationEmail = organizationEmail,
            CongressTitle = congressTitle,
            CongressLocation = congressLocation,
            CongressDateRange = congressDateRange,
            SubmissionCode = submissionNumber,
            AuthorFullName = authorFullName,
            SubmissionTitle = submission.Title,
            SubmissionTypeName = submissionTypeName,
            BodyContent = bodyContent,
            SignerFullName = signerName,
            SignerDuty = signerDuty,
            VerificationCode = verificationCode,
            VerificationUrl = verificationUrl,
            LogoBytes = resources.LogoBytes,
            SignatureBytes = resources.SignatureBytes,
            QrCodeBytes = qrCodeBytes,
            Culture = "en-US"
        };

        byte[] pdfBytes = _pdfRenderer.Render(pdfModel);

        await using MemoryStream pdfStream = new(pdfBytes);

        ObjectStorageUploadResult uploadResult = await _objectStorageService.UploadAsync(
            new ObjectStorageUploadRequest
            {
                BucketName = bucketName,
                ObjectName = objectName,
                OriginalFileName = fileName,
                ContentType = "application/pdf",
                Size = pdfBytes.LongLength,
                Content = pdfStream,
                Metadata = new Dictionary<string, string>
                {
                    ["module"] = "submission-acceptance-letter",
                    ["submission-id"] = submission.Id.ToString("D"),
                    ["author-id"] = author.Id.ToString("D")
                }
            },
            cancellationToken);

        string htmlSnapshot = BuildHtmlSnapshot(pdfModel);
        bool isNew = existing is null;

        SubmissionAcceptanceLetter letter = existing ?? new SubmissionAcceptanceLetter
        {
            Id = Guid.NewGuid(),
            SubmissionId = submission.Id,
            LanguageId = submission.LanguageId,
            AuthorId = author.Id,
            CreatedDate = now,
            CreatedBy = auditActor
        };

        letter.SubmissionId = submission.Id;
        letter.LanguageId = submission.LanguageId;
        letter.AuthorId = author.Id;
        letter.LetterNumber = verificationCode;
        letter.FileName = fileName;
        letter.AuthorFullNameSnapshot = authorFullName;
        letter.AuthorEmailSnapshot = NormalizeOptional(author.Email);
        letter.SignerBoardMemberId = resources.Signer.Member.Id;
        letter.SignerNameSnapshot = signerName;
        letter.SignerTitleSnapshot = signerDuty;
        letter.HtmlSnapshot = htmlSnapshot;
        letter.PdfFilePath = uploadResult.ObjectName;
        letter.StorageProvider = _storageOptions.Provider;
        letter.PdfBucketName = uploadResult.BucketName;
        letter.PdfObjectName = uploadResult.ObjectName;
        letter.PdfContentType = uploadResult.ContentType;
        letter.PdfFileSize = uploadResult.Size;
        letter.PdfETag = uploadResult.ETag;
        letter.GeneratedAt = now;
        letter.DeletedDate = null;
        letter.DeletedBy = null;
        letter.UpdatedDate = isNew ? null : now;
        letter.UpdatedBy = isNew ? null : auditActor;

        if (resetSentInfo)
        {
            letter.SentAt = null;
            letter.SentToEmail = null;
        }

        SubmissionAcceptanceLetter savedLetter = isNew
            ? await _acceptanceLetterRepository.AddAsync(letter)
            : await _acceptanceLetterRepository.UpdateAsync(letter);

        await AddSubmissionFileRecordAsync(submission.Id, savedLetter, now, auditActor, cancellationToken);

        return savedLetter;
    }

    private async Task AddSubmissionFileRecordAsync(
        Guid submissionId,
        SubmissionAcceptanceLetter letter,
        DateTime createdAt,
        string auditActor,
        CancellationToken cancellationToken)
    {
        string filePath = letter.PdfObjectName ?? letter.PdfFilePath ?? string.Empty;
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        SubmissionFile? existingFile = await _submissionFileRepository
            .Query()
            .Where(file =>
                file.SubmissionId == submissionId &&
                file.FileKind == SubmissionFileKind.AcceptanceLetter &&
                file.FilePath == filePath &&
                file.DeletedDate == null)
            .OrderByDescending(file => file.IsActive)
            .ThenByDescending(file => file.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingFile is not null)
        {
            existingFile.OriginalFileName = letter.FileName;
            existingFile.FilePath = filePath;
            existingFile.ContentType = letter.PdfContentType ?? "application/pdf";
            existingFile.FileSize = letter.PdfFileSize;
            existingFile.IsActive = true;
            existingFile.DeletedDate = null;
            existingFile.DeletedBy = null;
            existingFile.CreatedDate = createdAt;
            existingFile.UpdatedDate = createdAt;
            existingFile.UpdatedBy = auditActor;
            await _submissionFileRepository.UpdateAsync(existingFile);
            return;
        }

        SubmissionFile file = new()
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            FileKind = SubmissionFileKind.AcceptanceLetter,
            OriginalFileName = letter.FileName,
            FilePath = filePath,
            ContentType = letter.PdfContentType ?? "application/pdf",
            FileSize = letter.PdfFileSize,
            IsActive = true,
            CreatedDate = createdAt,
            CreatedBy = auditActor
        };

        await _submissionFileRepository.AddAsync(file);
    }

    private async Task<List<StorageObjectReference>> GetExistingAcceptanceLetterObjectReferencesAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        List<StorageObjectReference> previousObjects = new();
        string bucketName = GetSubmissionsBucketName();

        List<SubmissionFile> files = await _submissionFileRepository
            .Query()
            .Where(file =>
                file.SubmissionId == submissionId &&
                file.FileKind == SubmissionFileKind.AcceptanceLetter &&
                file.DeletedDate == null)
            .ToListAsync(cancellationToken);

        foreach (SubmissionFile file in files)
            AddObjectReference(previousObjects, bucketName, file.FilePath);

        return previousObjects;
    }

    private async Task DeactivateObsoleteAcceptanceLetterFileRecordsAsync(
        Guid submissionId,
        ISet<string> currentFilePaths,
        DateTime now,
        string auditActor,
        CancellationToken cancellationToken)
    {
        List<SubmissionFile> files = await _submissionFileRepository
            .Query()
            .Where(file =>
                file.SubmissionId == submissionId &&
                file.FileKind == SubmissionFileKind.AcceptanceLetter &&
                file.DeletedDate == null)
            .ToListAsync(cancellationToken);

        foreach (IGrouping<string, SubmissionFile> group in files.GroupBy(file => file.FilePath ?? string.Empty, StringComparer.Ordinal))
        {
            bool isCurrentPath = currentFilePaths.Contains(group.Key);
            SubmissionFile? keep = isCurrentPath
                ? group.OrderByDescending(file => file.CreatedDate).ThenByDescending(file => file.Id).FirstOrDefault()
                : null;

            foreach (SubmissionFile file in group)
            {
                if (keep is not null && file.Id == keep.Id)
                    continue;

                file.IsActive = false;
                file.DeletedDate = now;
                file.DeletedBy = auditActor;
                file.UpdatedDate = now;
                file.UpdatedBy = auditActor;
                await _submissionFileRepository.UpdateAsync(file);
            }
        }
    }

    private async Task<AcceptanceLetterResources> ResolveResourcesAsync(
        Submission aggregate,
        CancellationToken cancellationToken)
    {
        AcceptanceLetterSigner? signer = await ResolveSignerAsync(aggregate.CongressId, cancellationToken);
        if (signer is null)
            throw new InvalidOperationException("Acceptance letter signer could not be resolved. Configure the first active member of the organizing board with a valid signature.");

        byte[]? signatureBytes = await TryReadObjectAsync(
            signer.Member.SignatureBucketName,
            signer.Member.SignatureObjectName ?? signer.Member.SignaturePath,
            cancellationToken);

        if (signatureBytes is not { Length: > 0 })
            throw new InvalidOperationException("Acceptance letter signer signature could not be read from object storage.");

        byte[]? logoBytes = await TryResolveLogoBytesAsync(aggregate, cancellationToken);

        return new AcceptanceLetterResources(signer, logoBytes, signatureBytes);
    }

    private async Task<SubmissionAcceptanceLetter?> GetCurrentLetterAsync(
        Guid submissionId,
        Guid authorId,
        Guid? languageId,
        CancellationToken cancellationToken)
    {
        return await _acceptanceLetterRepository.GetAsync(
            predicate: letter =>
                letter.SubmissionId == submissionId &&
                letter.AuthorId == authorId &&
                letter.LanguageId == languageId &&
                letter.DeletedDate == null,
            cancellationToken: cancellationToken);
    }

    private static List<Author> GetActiveAuthors(Submission aggregate)
    {
        return aggregate.Authors
            .Where(author => author.DeletedDate is null)
            .OrderByDescending(author => author.IsCorrespondingAuthor)
            .ThenBy(author => author.LastName)
            .ThenBy(author => author.FirstName)
            .ToList();
    }

    private async Task DeletePreviousObjectsAsync(
        IEnumerable<StorageObjectReference> previousObjects,
        ISet<string> currentObjectKeys,
        CancellationToken cancellationToken)
    {
        foreach (StorageObjectReference previousObject in previousObjects
                     .Where(item => !string.IsNullOrWhiteSpace(item.BucketName) && !string.IsNullOrWhiteSpace(item.ObjectName))
                     .DistinctBy(item => BuildObjectReferenceKey(item.BucketName, item.ObjectName)))
        {
            if (currentObjectKeys.Contains(BuildObjectReferenceKey(previousObject.BucketName, previousObject.ObjectName)))
                continue;

            await BackOfficeObjectStorageHelper.DeleteObjectIfExistsAsync(
                _objectStorageService,
                previousObject.BucketName,
                previousObject.ObjectName,
                cancellationToken);
        }
    }

    private static void AddObjectReference(
        ICollection<StorageObjectReference> references,
        string? bucketName,
        string? objectName)
    {
        if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(objectName))
            return;

        references.Add(new StorageObjectReference(bucketName.Trim(), objectName.Trim()));
    }

    private static string BuildObjectReferenceKey(string? bucketName, string? objectName)
        => $"{bucketName?.Trim()}|{objectName?.Trim()}";

    private async Task<AcceptanceLetterSigner?> ResolveSignerAsync(Guid congressId, CancellationToken cancellationToken)
    {
        List<CongressBoard> boards = await _congressBoardRepository
            .Query()
            .Where(board => board.CongressId == congressId && board.DeletedDate == null)
            .OrderBy(board => board.Order <= 0 ? int.MaxValue : board.Order)
            .ThenBy(board => board.Id)
            .ToListAsync(cancellationToken);

        if (boards.Count == 0)
            return null;

        HashSet<Guid> boardIds = boards.Select(board => board.Id).ToHashSet();

        List<CongressBoardTranslation> boardTranslations = await _congressBoardTranslationRepository
            .Query()
            .Include(translation => translation.Language)
            .Where(translation => boardIds.Contains(translation.CongressBoardId) && translation.DeletedDate == null)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> boardNameMap = boards.ToDictionary(
            board => board.Id,
            board => ResolveBoardDisplayName(board.Id, boardTranslations));

        Dictionary<Guid, string> englishBoardNameMap = boards.ToDictionary(
            board => board.Id,
            board => ResolveEnglishBoardDisplayName(
                board.Id,
                boardTranslations,
                boardNameMap[board.Id]));

        bool hasExplicitOrganizingBoard = boardNameMap.Values.Any(IsOrganizingBoardName);
        Guid firstBoardId = boards.First().Id;
        Dictionary<Guid, int> boardOrderMap = boards.ToDictionary(
            board => board.Id,
            board => board.Order <= 0 ? int.MaxValue : board.Order);

        List<CongressBoardMember> members = await _congressBoardMemberRepository
            .Query()
            .Include(member => member.Translations)
            .Where(member =>
                boardIds.Contains(member.CongressBoardId) &&
                member.IsActive &&
                member.DeletedDate == null)
            .ToListAsync(cancellationToken);

        if (members.Count == 0)
            return null;

        List<SignerCandidate> candidates = members
            .Select(member =>
            {
                string boardName = boardNameMap.TryGetValue(member.CongressBoardId, out string? resolvedBoardName)
                    ? resolvedBoardName
                    : string.Empty;

                bool isPreferredBoard = hasExplicitOrganizingBoard
                    ? IsOrganizingBoardName(boardName)
                    : member.CongressBoardId == firstBoardId;

                int boardOrder = boardOrderMap.TryGetValue(member.CongressBoardId, out int resolvedOrder)
                    ? resolvedOrder
                    : int.MaxValue;

                string englishBoardName = englishBoardNameMap.TryGetValue(
                    member.CongressBoardId,
                    out string? resolvedEnglishBoardName)
                    ? resolvedEnglishBoardName
                    : boardName;

                return new SignerCandidate(
                    member,
                    boardName,
                    englishBoardName,
                    boardOrder,
                    isPreferredBoard);
            })
            .ToList();

        SignerCandidate? selected = SelectSignerCandidate(candidates);

        return selected is null
            ? null
            : new AcceptanceLetterSigner(
                selected.Member,
                FirstNonEmpty(
                    selected.EnglishBoardName,
                    selected.BoardName,
                    string.Empty));
    }

    private static SignerCandidate? SelectSignerCandidate(IReadOnlyCollection<SignerCandidate> candidates)
    {
        // Kongrede açıkça imza yetkisi verilen kişi kurul türünden bağımsız
        // olarak her zaman ilk tercih olmalıdır.
        return OrderSignerCandidates(
                candidates.Where(candidate =>
                    candidate.Member.IsAcceptanceLetterSigner &&
                    HasSignature(candidate.Member)))
            .FirstOrDefault()
            ?? OrderSignerCandidates(
                candidates.Where(candidate =>
                    candidate.Member.IsAcceptanceLetterSigner))
                .FirstOrDefault()
            ?? OrderSignerCandidates(
                candidates.Where(candidate =>
                    candidate.IsPreferredBoard &&
                    HasSignature(candidate.Member)))
                .FirstOrDefault()
            ?? OrderSignerCandidates(
                candidates.Where(candidate =>
                    HasSignature(candidate.Member)))
                .FirstOrDefault()
            ?? OrderSignerCandidates(
                candidates.Where(candidate =>
                    candidate.IsPreferredBoard))
                .FirstOrDefault()
            ?? OrderSignerCandidates(candidates)
                .FirstOrDefault();
    }

    private static IOrderedEnumerable<SignerCandidate> OrderSignerCandidates(IEnumerable<SignerCandidate> candidates)
        => candidates
            .OrderByDescending(candidate => candidate.Member.IsAcceptanceLetterSigner)
            .ThenBy(candidate => candidate.BoardOrder)
            .ThenBy(candidate => candidate.Member.Order <= 0 ? int.MaxValue : candidate.Member.Order)
            .ThenBy(candidate => candidate.Member.FullName);

    private static string ResolveBoardDisplayName(Guid boardId, IEnumerable<CongressBoardTranslation> translations)
    {
        List<CongressBoardTranslation> boardTranslations = translations
            .Where(translation => translation.CongressBoardId == boardId)
            .ToList();

        return boardTranslations
                   .Select(translation => NormalizeOptional(translation.Name))
                   .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value) && IsOrganizingBoardName(value))
               ?? boardTranslations
                   .Select(translation => NormalizeOptional(translation.Name))
                   .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
               ?? "Organizing Committee";
    }

    private static string ResolveEnglishBoardDisplayName(
        Guid boardId,
        IEnumerable<CongressBoardTranslation> translations,
        string fallbackBoardName)
    {
        List<CongressBoardTranslation> boardTranslations = translations
            .Where(translation => translation.CongressBoardId == boardId)
            .ToList();

        string? englishName = boardTranslations
            .Where(translation => IsEnglishCulture(translation.Language?.Culture))
            .Select(translation => NormalizeOptional(translation.Name))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        // PDF görevi statik değildir. İmza yetkilisinin bağlı olduğu kurulun
        // İngilizce çevirisi kullanılır. EN çeviri yoksa mevcut kurul adı
        // fallback olarak gösterilir.
        return FirstNonEmpty(
                   englishName,
                   fallbackBoardName,
                   string.Empty)
               ?? string.Empty;
    }

    private static bool IsOrganizingBoardName(string? value)
    {
        string normalized = NormalizeTextForSearch(value);
        return normalized.Contains("duzenleme", StringComparison.Ordinal) ||
               normalized.Contains("organizing", StringComparison.Ordinal) ||
               normalized.Contains("organisation", StringComparison.Ordinal) ||
               normalized.Contains("organization", StringComparison.Ordinal) ||
               normalized.Contains("editorial", StringComparison.Ordinal);
    }

    private async Task<byte[]?> TryResolveLogoBytesAsync(Submission submission, CancellationToken cancellationToken)
    {
        string? bucketName = GetCongressImagesBucketNameOrNull();
        if (string.IsNullOrWhiteSpace(bucketName))
            return null;

        CongressTranslation? submissionLanguageTranslation = submission.LanguageId.HasValue
            ? submission.Congress.Translations.FirstOrDefault(translation => translation.LanguageId == submission.LanguageId.Value)
            : null;

        string?[] logoCandidates =
        {
            submission.Congress.Organization?.LogoLightPath,
            submission.Congress.Organization?.LogoDarkPath,
            submission.Congress.LogoLightPath,
            submission.Congress.LogoDarkPath,
            submissionLanguageTranslation?.LogoPath,
            submission.Congress.Translations.FirstOrDefault()?.LogoPath
        };

        foreach (string? logoCandidate in logoCandidates)
        {
            byte[]? bytes = await TryReadObjectAsync(bucketName, logoCandidate, cancellationToken);
            if (bytes is { Length: > 0 })
                return bytes;
        }

        return null;
    }

    private async Task<byte[]?> TryReadObjectAsync(string? bucketName, string? objectName, CancellationToken cancellationToken)
    {
        string? normalizedObjectName = NormalizeStorageObjectName(bucketName, objectName);
        if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(normalizedObjectName))
            return null;

        try
        {
            await using Stream stream = await _objectStorageService.OpenReadAsync(bucketName.Trim(), normalizedObjectName, cancellationToken);
            using MemoryStream memoryStream = new();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            return memoryStream.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static string? NormalizeStorageObjectName(string? bucketName, string? value)
    {
        if (string.IsNullOrWhiteSpace(bucketName) || string.IsNullOrWhiteSpace(value))
            return null;

        string normalizedBucketName = bucketName.Trim().Trim('/');
        string normalizedValue = value.Trim();

        if (Uri.TryCreate(normalizedValue, UriKind.Absolute, out Uri? uri))
            return ExtractObjectNameFromPath(normalizedBucketName, uri.AbsolutePath);

        if (normalizedValue.StartsWith("~/", StringComparison.Ordinal))
            return null;

        if (normalizedValue.StartsWith("/", StringComparison.Ordinal))
            return ExtractObjectNameFromPath(normalizedBucketName, normalizedValue);

        normalizedValue = Uri.UnescapeDataString(normalizedValue.Replace('\\', '/')).TrimStart('/');

        if (normalizedValue.StartsWith("storage/public/", StringComparison.OrdinalIgnoreCase) ||
            normalizedValue.StartsWith("public-assets/", StringComparison.OrdinalIgnoreCase))
            return ExtractObjectNameFromPath(normalizedBucketName, normalizedValue);

        if (normalizedValue.StartsWith(normalizedBucketName + "/", StringComparison.OrdinalIgnoreCase))
            return normalizedValue[(normalizedBucketName.Length + 1)..];

        return normalizedValue;
    }

    private static string? ExtractObjectNameFromPath(string bucketName, string path)
    {
        string normalizedPath = Uri.UnescapeDataString(path.Replace('\\', '/')).Trim('/');
        if (string.IsNullOrWhiteSpace(normalizedPath))
            return null;

        string[] segments = normalizedPath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length >= 3 && string.Equals(segments[0], "public-assets", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(segments[1], bucketName, StringComparison.OrdinalIgnoreCase)
                ? string.Join('/', segments.Skip(2))
                : null;
        }

        if (segments.Length >= 3 &&
            string.Equals(segments[0], "storage", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(segments[1], "public", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(segments[2], bucketName, StringComparison.OrdinalIgnoreCase))
                return string.Join('/', segments.Skip(3));

            if (IsPublicStorageAliasForBucket(segments[2], bucketName))
                return string.Join('/', segments.Skip(3));
        }

        if (segments.Length >= 2 && string.Equals(segments[0], bucketName, StringComparison.OrdinalIgnoreCase))
            return string.Join('/', segments.Skip(1));

        return null;
    }

    private static bool IsPublicStorageAliasForBucket(string alias, string bucketName)
    {
        string normalizedAlias = NormalizeTextForSearch(alias);
        string normalizedBucket = NormalizeTextForSearch(bucketName);

        if (string.IsNullOrWhiteSpace(normalizedAlias) || string.IsNullOrWhiteSpace(normalizedBucket))
            return false;

        if (normalizedAlias == normalizedBucket)
            return true;

        return (normalizedAlias is "congress" or "congressimage" or "congressimages" or "image" or "images") &&
               normalizedBucket.Contains("congress", StringComparison.Ordinal) &&
               normalizedBucket.Contains("image", StringComparison.Ordinal);
    }

    private string GetSubmissionsBucketName()
    {
        if (string.IsNullOrWhiteSpace(_storageOptions.Buckets.Submissions))
            throw new InvalidOperationException("ObjectStorage:Bucket:Submissions configuration is required for acceptance letters.");

        return _storageOptions.Buckets.Submissions.Trim();
    }

    private string? GetCongressImagesBucketNameOrNull()
        => string.IsNullOrWhiteSpace(_storageOptions.Buckets.CongressImages)
            ? null
            : _storageOptions.Buckets.CongressImages.Trim();

    private static string BuildObjectName(string submissionNumber, Guid authorId, string fileName)
        => string.Join('/', new[]
        {
            "submissions",
            Slug(submissionNumber),
            "acceptance-letters",
            authorId.ToString("N"),
            fileName
        });

    private static string BuildHtmlSnapshot(AcceptanceLetterPdfModel model)
    {
        string[] bodyParagraphs = model.BodyContent
            .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        string bodyHtml = string.Join(
            Environment.NewLine,
            bodyParagraphs.Select(paragraph => $"<p>{WebUtility.HtmlEncode(paragraph)}</p>"));

        string organizationEmailHtml = string.IsNullOrWhiteSpace(model.OrganizationEmail)
            ? string.Empty
            : $"<p><strong>Contact:</strong> {WebUtility.HtmlEncode(model.OrganizationEmail)}</p>";

        return $"""
            <section class=""acceptance-letter"">
                <header>
                    <h1>ACCEPTANCE LETTER</h1>
                    <h2>{WebUtility.HtmlEncode(model.CongressTitle)}</h2>
                    <p>{WebUtility.HtmlEncode(model.CongressLocation)}</p>
                    <p>{WebUtility.HtmlEncode(model.CongressDateRange)}</p>
                </header>
                <p><strong>Submission Code:</strong> {WebUtility.HtmlEncode(model.SubmissionCode)}</p>
                {bodyHtml}
                <section class=""signature"">
                    <p><strong>{WebUtility.HtmlEncode(model.SignerFullName)}</strong></p>
                    <p>{WebUtility.HtmlEncode(model.SignerDuty)}</p>
                </section>
                <footer>
                    {organizationEmailHtml}
                    <p><strong>Document Verification</strong></p>
                    <p>Verification Code: {WebUtility.HtmlEncode(model.VerificationCode)}</p>
                    <p>Verify: {WebUtility.HtmlEncode(model.VerificationUrl)}</p>
                </footer>
            </section>
            """;
    }

    private static string BuildBodyContent(
        string authorFullName,
        string submissionTitle,
        string submissionTypeName,
        string congressTitle,
        string congressDateRange,
        string organizationShortName)
    {
        string normalizedAuthorFullName = FirstNonEmpty(authorFullName, "Author");
        string normalizedSubmissionTitle = FirstNonEmpty(submissionTitle, "Untitled Submission");
        string normalizedSubmissionType = FirstNonEmpty(submissionTypeName, "Paper");
        string normalizedCongressTitle = FirstNonEmpty(congressTitle, "the congress");
        string normalizedCongressDateRange = FirstNonEmpty(congressDateRange, "the announced congress dates");
        string normalizedOrganizationShortName = FirstNonEmpty(organizationShortName, "congress");

        return string.Join(
            $"{Environment.NewLine}{Environment.NewLine}",
            $"Dear {normalizedAuthorFullName},",
            $"Your application for {normalizedSubmissionType} with the theme \"{normalizedSubmissionTitle}\" to be presented at the {normalizedCongressTitle} to be held between {normalizedCongressDateRange} was accepted after the review and editorial approval process. Preparation guidelines are available through the official {normalizedOrganizationShortName} announcements.",
            "Thank you for your interest and we wish you continued success in your academic work.");
    }

    private static string ResolveSubmissionNumber(Submission submission)
    {
        string value = NormalizeOptional(submission.SubmissionNumber) ?? string.Empty;
        return string.IsNullOrWhiteSpace(value)
            ? BuildCompactSubmissionCode(submission.Id)
            : value.Trim().ToUpperInvariant();
    }

    private static string BuildCompactSubmissionCode(Guid id)
        => id.ToString("N")[..8].ToUpperInvariant();

    private static string ResolveCongressShortName(Congress congress)
    {
        string? code = NormalizeOptional(congress.Code);
        if (string.IsNullOrWhiteSpace(code))
            return "UTSAK";

        int dashIndex = code.IndexOf('-', StringComparison.Ordinal);
        return dashIndex > 0 ? code[..dashIndex].ToUpperInvariant() : code.ToUpperInvariant();
    }

    private static string ResolveOrganizationShortName(Congress congress)
        => NormalizeOptional(congress.Organization?.ShortName)?.ToUpperInvariant()
           ?? ResolveCongressShortName(congress);

    private static string ResolveOrganizationName(Congress congress)
        => NormalizeOptional(congress.Organization?.Name)
           ?? NormalizeOptional(congress.Name)
           ?? ResolveOrganizationShortName(congress);

    private static string ResolveCongressEmails(Congress congress)
    {
        List<string> emails = congress.ContactEmails
            .Where(item =>
                item.DeletedDate == null &&
                !string.IsNullOrWhiteSpace(item.Email))
            .OrderByDescending(item => item.IsPrimary)
            .ThenBy(item => item.Order <= 0 ? int.MaxValue : item.Order)
            .ThenBy(item => item.Email)
            .Select(item => item.Email.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (emails.Count == 0)
        {
            string? legacyCongressEmail = NormalizeOptional(congress.ContactEmail);
            if (!string.IsNullOrWhiteSpace(legacyCongressEmail))
                emails.Add(legacyCongressEmail);
        }

        if (emails.Count == 0)
        {
            string? organizationEmail = NormalizeOptional(congress.Organization?.ContactEmail);
            if (!string.IsNullOrWhiteSpace(organizationEmail))
                emails.Add(organizationEmail);
        }

        return string.Join(Environment.NewLine, emails);
    }

    private async Task<string> ResolveCongressLocationAsync(
        Congress congress,
        CancellationToken cancellationToken)
    {
        const string targetCulture = "en-US";

        Guid? stateId = congress.StateId;
        Guid? countryId = congress.CountryId;

        // State seçilmiş ancak eski/legacy kayıtta CountryId boş kalmışsa
        // State.CountryId üzerinden country'yi tamamla.
        if (stateId.HasValue && !countryId.HasValue)
        {
            Guid? resolvedCountryId = await _stateRepository
                .Query()
                .AsNoTracking()
                .Where(state =>
                    state.Id == stateId.Value &&
                    state.DeletedDate == null)
                .Select(state => (Guid?)state.CountryId)
                .FirstOrDefaultAsync(cancellationToken);

            countryId = resolvedCountryId;
        }

        string? stateName = stateId.HasValue
            ? await _stateTranslationRepository
                .Query()
                .AsNoTracking()
                .Where(translation =>
                    translation.StateId == stateId.Value &&
                    translation.DeletedDate == null)
                .Include(translation => translation.Language)
                .OrderByDescending(translation =>
                    translation.Language != null &&
                    translation.Language.Culture == targetCulture)
                .ThenByDescending(translation =>
                    translation.Language != null &&
                    translation.Language.IsDefault)
                .ThenBy(translation =>
                    translation.Language != null
                        ? translation.Language.Order
                        : int.MaxValue)
                .Select(translation => translation.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        string? countryName = countryId.HasValue
            ? await _countryTranslationRepository
                .Query()
                .AsNoTracking()
                .Where(translation =>
                    translation.CountryId == countryId.Value &&
                    translation.DeletedDate == null)
                .Include(translation => translation.Language)
                .OrderByDescending(translation =>
                    translation.Language != null &&
                    translation.Language.Culture == targetCulture)
                .ThenByDescending(translation =>
                    translation.Language != null &&
                    translation.Language.IsDefault)
                .ThenBy(translation =>
                    translation.Language != null
                        ? translation.Language.Order
                        : int.MaxValue)
                .Select(translation => translation.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        string[] parts =
        {
            NormalizeOptional(stateName) ?? string.Empty,
            NormalizeOptional(countryName) ?? string.Empty
        };

        // Kabul mektubunda yalnızca İl/Eyalet + Ülke gösterilir.
        // VenueName, ContactAddress ve City burada bilinçli olarak kullanılmaz.
        return string.Join(
            " / ",
            parts.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string ResolveSignerDisplayName(CongressBoardMember? signer, Congress congress, string? academicTitleOverride)
    {
        string? signerName = NormalizeOptional(signer?.FullName) ?? NormalizeOptional(congress.ContactName);
        string? academicTitle = NormalizeOptional(academicTitleOverride) ?? NormalizeOptional(signer?.AcademicTitle);

        if (string.IsNullOrWhiteSpace(signerName))
            return "Congress Secretariat";

        return string.IsNullOrWhiteSpace(academicTitle)
            ? signerName
            : $"{academicTitle} {signerName}";
    }

    private async Task<string> ResolveSignerAcademicTitleAsync(string? academicTitle, Guid? languageId, CancellationToken cancellationToken)
    {
        string? normalizedAcademicTitle = NormalizeOptional(academicTitle);
        if (string.IsNullOrWhiteSpace(normalizedAcademicTitle))
            return string.Empty;

        string lookupKey = NormalizeTextForSearch(normalizedAcademicTitle);

        var translations = await _titleTranslationRepository
            .Query()
            .Where(translation => translation.DeletedDate == null)
            .Select(translation => new
            {
                translation.LanguageId,
                translation.Name,
                translation.Description
            })
            .ToListAsync(cancellationToken);

        var matchedTranslation = translations
            .Where(translation =>
                NormalizeTextForSearch(translation.Name) == lookupKey ||
                NormalizeTextForSearch(translation.Description) == lookupKey)
            .OrderByDescending(translation => translation.LanguageId == languageId)
            .ThenBy(translation => string.IsNullOrWhiteSpace(translation.Description) ? 1 : 0)
            .FirstOrDefault();

        return FirstNonEmpty(
            matchedTranslation?.Description,
            matchedTranslation?.Name,
            normalizedAcademicTitle);
    }

    private static string ResolveSignerDuty(AcceptanceLetterSigner signer, Congress congress)
    {
        _ = congress;

        // Duty, seçilen imza yetkilisinin bağlı olduğu CongressBoard kaydının
        // en-US çevirisinden ResolveSignerAsync içinde gelir.
        return FirstNonEmpty(
                   signer.Duty,
                   string.Empty)
               ?? string.Empty;
    }

    private string BuildVerificationUrl(string verificationCode)
    {
        return _publicUrlService.Build(
            $"/verify/acceptance-letter/{Uri.EscapeDataString(verificationCode)}");
    }

    private static string BuildVerificationCode(string organizationShortName, string submissionNumber, Guid authorId)
    {
        string authorToken = authorId.ToString("N")[^12..].ToUpperInvariant();
        return $"AL-{Slug(organizationShortName).ToUpperInvariant()}-{Slug(submissionNumber).ToUpperInvariant()}-{authorToken}";
    }

    private static byte[]? TryGenerateQrCodeBytes(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        try
        {
            using QRCodeGenerator generator = new();
            using QRCodeData data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            using PngByteQRCode qrCode = new(data);
            return qrCode.GetGraphic(8);
        }
        catch
        {
            return null;
        }
    }

    private static string ResolveEnglishCongressTitle(Submission submission)
        => submission.Congress.Translations
               .Where(translation => translation.DeletedDate == null)
               .OrderByDescending(translation => IsEnglishCulture(translation.Language?.Culture))
               .ThenByDescending(translation => translation.LanguageId == submission.LanguageId)
               .Select(translation => NormalizeOptional(translation.Title))
               .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
           ?? NormalizeOptional(submission.Congress.Name)
           ?? NormalizeOptional(submission.Congress.Code)
           ?? "Congress";

    private static string ResolveEnglishSubmissionTypeName(Submission submission)
        => submission.SubmissionType?.Translations
               .Where(translation => translation.DeletedDate == null)
               .OrderByDescending(translation => IsEnglishCulture(translation.Language?.Culture))
               .ThenByDescending(translation => translation.LanguageId == submission.LanguageId)
               .Select(translation => NormalizeOptional(translation.Name))
               .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
           ?? "Paper";

    private static bool IsEnglishCulture(string? culture)
        => !string.IsNullOrWhiteSpace(culture) && culture.StartsWith("en", StringComparison.OrdinalIgnoreCase);

    private static string ResolveEnglishCongressDateRange(Congress congress)
    {
        DateTime? start = NormalizeDate(congress.StartDate);
        DateTime? end = NormalizeDate(congress.EndDate);

        if (start is null && end is null)
            return string.Empty;

        if (start is not null && end is not null)
        {
            if (start.Value.Date == end.Value.Date)
                return FormatEnglishDate(start.Value);

            if (start.Value.Year == end.Value.Year && start.Value.Month == end.Value.Month)
                return $"{start.Value:dd}-{end.Value:dd} {start.Value.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("en-US"))}";

            return $"{FormatEnglishDate(start.Value)} - {FormatEnglishDate(end.Value)}";
        }

        return FormatEnglishDate((start ?? end)!.Value);
    }

    private static string FormatEnglishDate(DateTime value)
        => value.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("en-US"));

    private static DateTime? NormalizeDate(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return value.Value.Year < 1900 ? null : value.Value;
    }

    private static string ResolveAuthorDisplayName(Author author, Guid? languageId)
    {
        string authorName = NormalizePersonName(author);
        string authorTitle = ResolveAuthorTitle(author, languageId);

        return string.IsNullOrWhiteSpace(authorTitle)
            ? authorName
            : $"{authorTitle} {authorName}";
    }

    private static string ResolveAuthorTitle(Author author, Guid? languageId)
    {
        if (author.Title is null)
            return string.Empty;

        var preferredTranslation = author.Title.Translations
            .Where(translation => translation.DeletedDate == null)
            .OrderByDescending(translation => languageId.HasValue && translation.LanguageId == languageId.Value)
            .ThenBy(translation => string.IsNullOrWhiteSpace(translation.Description) ? 1 : 0)
            .ThenBy(translation => string.IsNullOrWhiteSpace(translation.Name) ? 1 : 0)
            .FirstOrDefault();

        return FirstNonEmpty(
            preferredTranslation?.Description,
            preferredTranslation?.Name,
            author.Title.Code);
    }

    private static string NormalizePersonName(Author author)
    {
        string fullName = $"{NormalizeOptional(author.FirstName)} {NormalizeOptional(author.LastName)}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? "Author" : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fullName.ToLowerInvariant());
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim()
           ?? string.Empty;

    private static string NormalizeTextForSearch(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : Slug(value);

    private static bool HasSignature(CongressBoardMember member)
        => !string.IsNullOrWhiteSpace(member.SignatureObjectName) || !string.IsNullOrWhiteSpace(member.SignaturePath);

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

        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }

    private sealed record AcceptanceLetterResources(AcceptanceLetterSigner Signer, byte[]? LogoBytes, byte[] SignatureBytes);

    private sealed record StorageObjectReference(string BucketName, string ObjectName);

    private sealed record AcceptanceLetterSigner(CongressBoardMember Member, string Duty);

    private sealed record SignerCandidate(
        CongressBoardMember Member,
        string BoardName,
        string EnglishBoardName,
        int BoardOrder,
        bool IsPreferredBoard);
}
