using Core.Application.Pipelines.Authorization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Features.Submissions.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.Workflow;
using SubmissionFileKind = Symplify.BackOffice.Domain.Enums.SubmissionFileKind;
using SubmissionFormProfile = Symplify.BackOffice.Domain.Enums.SubmissionFormProfile;

namespace Symplify.BackOffice.Application.Features.Submissions.Queries.GetById;

public sealed class GetByIdSubmissionQuery : IRequest<GetByIdSubmissionResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public string? Culture { get; set; }

    public string[] Roles => new[]
    {
        SubmissionsOperationClaims.Admin,
        SubmissionsOperationClaims.Read
    };

    public sealed class GetByIdSubmissionQueryHandler : IRequestHandler<GetByIdSubmissionQuery, GetByIdSubmissionResponse>
    {
        private const int MaxHistoryRows = 100;

        private readonly ISubmissionRepository _repository;
        private readonly ISubmissionAcceptanceLetterRepository _acceptanceLetterRepository;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly SubmissionBusinessRules _rules;

        public GetByIdSubmissionQueryHandler(
            ISubmissionRepository repository,
            ISubmissionAcceptanceLetterRepository acceptanceLetterRepository,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            SubmissionBusinessRules rules)
        {
            _repository = repository;
            _acceptanceLetterRepository = acceptanceLetterRepository;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _rules = rules;
        }

        public async Task<GetByIdSubmissionResponse> Handle(GetByIdSubmissionQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.Culture, defaultLanguage, cancellationToken);

            SubmissionHeaderProjection? header = await GetHeaderAsync(request.Id, requestedLanguage.Id, defaultLanguage.Id, cancellationToken);
            if (header is null)
            {
                await _rules.SubmissionShouldExistWhenSelected(null);
                throw new InvalidOperationException("Submission existence rule did not throw.");
            }

            List<SubmissionDetailAuthorDto> authors = await GetAuthorsAsync(request.Id, requestedLanguage.Id, defaultLanguage.Id, cancellationToken);
            List<SubmissionDetailReviewDto> reviews = await GetReviewsAsync(request.Id, requestedLanguage.Id, defaultLanguage.Id, cancellationToken);
            List<SubmissionDetailHistoryDto> histories = await GetHistoriesAsync(request.Id, requestedLanguage.Id, defaultLanguage.Id, cancellationToken);
            List<SubmissionDetailFileDto> fileDtos = await GetFilesAsync(request.Id, cancellationToken);
            List<AcceptanceLetterFileProjection> latestAcceptanceLetters = await GetLatestAcceptanceLettersAsync(request.Id, cancellationToken);

            fileDtos = BuildFileDtos(fileDtos, latestAcceptanceLetters);

            bool canEdit = IsEditableByAuthor(header.TransactionStatus, header.IsSubmitted);
            bool hasAuthorAction = header.IsSubmitted && canEdit;
            bool isDecisionCompleted = IsDecisionCompleted(header.TransactionStatus);
            string paymentStatusName = header.PaymentStatusName ?? header.PaymentStatus?.Code ?? "-";
            string transactionStatusName = header.TransactionStatusName ?? header.TransactionStatus?.Code ?? string.Empty;

            return new GetByIdSubmissionResponse
            {
                Id = header.Id,
                CongressId = header.CongressId,
                SubmissionTypeId = header.SubmissionTypeId,
                FormProfile = header.FormProfile,
                ExhibitionDetail = header.ExhibitionDetail,
                TopicId = header.TopicId,
                CreatedByUserId = header.CreatedByUserId,
                LanguageId = header.LanguageId,
                PaymentStatusId = header.PaymentStatusId,
                TransactionStatusId = header.TransactionStatusId,
                SubmissionNumber = header.SubmissionNumber,
                Orcid = header.Orcid,
                Title = header.Title,
                TitleEn = header.TitleEn,
                Abstract = header.Abstract,
                AbstractEn = header.AbstractEn,
                Keywords = header.Keywords,
                KeywordsEn = header.KeywordsEn,
                IsSubmitted = header.IsSubmitted,
                SubmittedAt = header.SubmittedAt,
                CreatedDate = header.CreatedDate,
                UpdatedDate = header.UpdatedDate,
                CongressName = header.CongressName,
                SubmissionTypeName = header.SubmissionTypeName ?? header.SubmissionTypeCode ?? "-",
                TopicName = header.TopicName ?? header.TopicCode ?? "-",
                LanguageName = header.LanguageName ?? requestedLanguage.Name,
                PaymentStatusName = paymentStatusName,
                PaymentStatusCode = header.PaymentStatus?.Code ?? string.Empty,
                TransactionStatusName = transactionStatusName,
                TransactionStatusCode = header.TransactionStatus?.Code ?? string.Empty,
                PaymentStatusBadgeClass = ResolvePaymentBadgeClass(header.PaymentStatus, paymentStatusName),
                TransactionStatusBadgeClass = ResolveTransactionBadgeClass(header.TransactionStatus, header.IsSubmitted),
                CanEdit = canEdit,
                HasAuthorAction = hasAuthorAction,
                IsDecisionCompleted = isDecisionCompleted,
                AuthorActionTitle = string.Empty,
                AuthorActionDescription = string.Empty,
                AuthorActionDueDate = null,
                CorrespondingAuthorName = authors.FirstOrDefault(author => author.IsCorrespondingAuthor)?.FullName,
                ReviewerCount = header.ReviewerCount,
                CompletedEvaluationCount = reviews.Count(review => review.CompletedAt.HasValue),
                FileCount = fileDtos.Count,
                LatestFileName = fileDtos.FirstOrDefault()?.OriginalFileName,
                CanUploadPaymentDocument = false,
                Authors = authors,
                Reviews = reviews,
                Files = fileDtos,
                PaymentDocuments = Array.Empty<SubmissionDetailPaymentDocumentDto>(),
                Histories = histories
            };
        }

        private async Task<SubmissionHeaderProjection?> GetHeaderAsync(
            Guid submissionId,
            Guid requestedLanguageId,
            Guid defaultLanguageId,
            CancellationToken cancellationToken)
        {
            return await _repository
                .Query()
                .AsNoTracking()
                .Where(item => item.Id == submissionId)
                .Select(item => new SubmissionHeaderProjection
                {
                    Id = item.Id,
                    CongressId = item.CongressId,
                    SubmissionTypeId = item.SubmissionTypeId,
                    TopicId = item.TopicId,
                    CreatedByUserId = item.CreatedByUserId,
                    LanguageId = item.LanguageId,
                    PaymentStatusId = item.PaymentStatusId,
                    TransactionStatusId = item.TransactionStatusId,
                    SubmissionNumber = item.SubmissionNumber,
                    Orcid = item.Orcid,
                    Title = item.Title,
                    TitleEn = item.TitleEn,
                    Abstract = item.Abstract,
                    AbstractEn = item.AbstractEn,
                    Keywords = item.Keywords,
                    KeywordsEn = item.KeywordsEn,
                    IsSubmitted = item.IsSubmitted,
                    SubmittedAt = item.SubmittedAt,
                    CreatedDate = item.CreatedDate,
                    UpdatedDate = item.UpdatedDate,
                    CongressName = item.Congress.Translations
                        .Where(translation => translation.DeletedDate == null && translation.LanguageId == requestedLanguageId)
                        .Select(translation => translation.Title)
                        .FirstOrDefault()
                        ?? item.Congress.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == defaultLanguageId)
                            .Select(translation => translation.Title)
                            .FirstOrDefault()
                        ?? item.Congress.Name,
                    LanguageName = item.Language != null ? item.Language.Name : null,
                    SubmissionTypeCode = item.SubmissionType != null ? item.SubmissionType.Code : null,
                    SubmissionTypeName = item.SubmissionType == null
                        ? null
                        : item.SubmissionType.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == requestedLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault()
                          ?? item.SubmissionType.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == defaultLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault(),
                    FormProfile = item.SubmissionType == null
                        ? SubmissionFormProfile.AcademicAbstract
                        : item.SubmissionType.FormProfile,
                    ExhibitionDetail = item.ExhibitionDetail == null
                        ? null
                        : new SubmissionDetailExhibitionDto
                        {
                            WorkName = item.ExhibitionDetail.WorkName,
                            Dimensions = item.ExhibitionDetail.Dimensions,
                            Technique = item.ExhibitionDetail.Technique,
                            Description = item.ExhibitionDetail.Description,
                            Address = item.ExhibitionDetail.Address
                        },
                    TopicCode = item.Topic != null ? item.Topic.Code : null,
                    TopicName = item.Topic == null
                        ? null
                        : item.Topic.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == requestedLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault()
                          ?? item.Topic.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == defaultLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault(),
                    PaymentStatus = item.PaymentStatus == null
                        ? null
                        : new StatusProjection
                        {
                            Id = item.PaymentStatus.Id,
                            Code = item.PaymentStatus.Code,
                            IsActive = item.PaymentStatus.IsActive,
                            IsEditable = null,
                            IsFinal = null,
                            DeletedDate = item.PaymentStatus.DeletedDate
                        },
                    PaymentStatusName = item.PaymentStatus == null
                        ? null
                        : item.PaymentStatus.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == requestedLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault()
                          ?? item.PaymentStatus.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == defaultLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault(),
                    TransactionStatus = item.TransactionStatus == null
                        ? null
                        : new StatusProjection
                        {
                            Id = item.TransactionStatus.Id,
                            Code = item.TransactionStatus.Code,
                            IsActive = item.TransactionStatus.IsActive,
                            IsEditable = item.TransactionStatus.IsEditable,
                            IsFinal = item.TransactionStatus.IsFinal,
                            DeletedDate = item.TransactionStatus.DeletedDate
                        },
                    TransactionStatusName = item.TransactionStatus == null
                        ? null
                        : item.TransactionStatus.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == requestedLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault()
                          ?? item.TransactionStatus.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == defaultLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault(),
                    ReviewerCount = item.Reviewers.Count(reviewer => reviewer.DeletedDate == null)
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<List<SubmissionDetailAuthorDto>> GetAuthorsAsync(
            Guid submissionId,
            Guid requestedLanguageId,
            Guid defaultLanguageId,
            CancellationToken cancellationToken)
        {
            List<AuthorProjection> rows = await _repository
                .Query()
                .AsNoTracking()
                .Where(submission => submission.Id == submissionId)
                .SelectMany(submission => submission.Authors)
                .Where(author => author.DeletedDate == null)
                .OrderByDescending(author => author.IsCorrespondingAuthor)
                .ThenBy(author => author.FirstName)
                .ThenBy(author => author.LastName)
                .Select(author => new AuthorProjection
                {
                    Id = author.Id,
                    TitleId = author.TitleId,
                    FirstName = author.FirstName,
                    LastName = author.LastName,
                    Email = author.Email,
                    Institution = author.Institution,
                    Orcid = author.Orcid,
                    IsCorrespondingAuthor = author.IsCorrespondingAuthor,
                    TitleCode = author.Title != null ? author.Title.Code : null,
                    RequestedTitleDescription = author.Title == null
                        ? null
                        : author.Title.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == requestedLanguageId)
                            .Select(translation => translation.Description)
                            .FirstOrDefault(),
                    RequestedTitleName = author.Title == null
                        ? null
                        : author.Title.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == requestedLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault(),
                    DefaultTitleDescription = author.Title == null
                        ? null
                        : author.Title.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == defaultLanguageId)
                            .Select(translation => translation.Description)
                            .FirstOrDefault(),
                    DefaultTitleName = author.Title == null
                        ? null
                        : author.Title.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == defaultLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            return rows
                .Select(author => new SubmissionDetailAuthorDto
                {
                    Id = author.Id,
                    TitleId = author.TitleId,
                    TitleName = ResolveTitleDisplayName(author),
                    FirstName = author.FirstName,
                    LastName = author.LastName,
                    FullName = JoinFullName(author.FirstName, author.LastName),
                    Email = author.Email,
                    Institution = author.Institution,
                    Orcid = author.Orcid,
                    IsCorrespondingAuthor = author.IsCorrespondingAuthor
                })
                .ToList();
        }

        private async Task<List<SubmissionDetailReviewDto>> GetReviewsAsync(
            Guid submissionId,
            Guid requestedLanguageId,
            Guid defaultLanguageId,
            CancellationToken cancellationToken)
        {
            List<EvaluationProjection> evaluations = await _repository
                .Query()
                .AsNoTracking()
                .Where(submission => submission.Id == submissionId)
                .SelectMany(submission => submission.Evaluations)
                .Where(evaluation => evaluation.DeletedDate == null)
                .OrderByDescending(evaluation => evaluation.CompletedAt ?? evaluation.UpdatedDate ?? evaluation.CreatedDate)
                .Select(evaluation => new EvaluationProjection
                {
                    Id = evaluation.Id,
                    Recommendation = evaluation.Recommendation,
                    Comment = evaluation.Comment,
                    TotalScore = evaluation.TotalScore,
                    CompletedAt = evaluation.CompletedAt,
                    CreatedDate = evaluation.CreatedDate
                })
                .ToListAsync(cancellationToken);

            if (evaluations.Count == 0)
                return new List<SubmissionDetailReviewDto>();

            List<Guid> evaluationIds = evaluations.Select(evaluation => evaluation.Id).ToList();

            List<EvaluationScoreProjection> scores = await _repository
                .Query()
                .AsNoTracking()
                .Where(submission => submission.Id == submissionId)
                .SelectMany(submission => submission.Evaluations)
                .Where(evaluation => evaluationIds.Contains(evaluation.Id))
                .SelectMany(evaluation => evaluation.Scores)
                .Where(score => score.DeletedDate == null)
                .OrderBy(score => score.EvaluationCriterion.Order)
                .ThenBy(score => score.EvaluationCriterion.Code)
                .Select(score => new EvaluationScoreProjection
                {
                    Id = score.Id,
                    EvaluationId = score.SubmissionEvaluationId,
                    CriterionCode = score.EvaluationCriterion.Code,
                    CriterionName = score.EvaluationCriterion.Translations
                        .Where(translation => translation.DeletedDate == null && translation.LanguageId == requestedLanguageId)
                        .Select(translation => translation.Name)
                        .FirstOrDefault()
                        ?? score.EvaluationCriterion.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == defaultLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault(),
                    Score = score.Score,
                    Comment = score.Comment
                })
                .ToListAsync(cancellationToken);

            Dictionary<Guid, List<EvaluationScoreProjection>> scoreLookup = scores
                .GroupBy(score => score.EvaluationId)
                .ToDictionary(group => group.Key, group => group.ToList());

            return evaluations
                .Select((evaluation, index) =>
                {
                    List<EvaluationScoreProjection> evaluationScores = scoreLookup.TryGetValue(evaluation.Id, out List<EvaluationScoreProjection>? foundScores)
                        ? foundScores
                        : new List<EvaluationScoreProjection>();

                    return new SubmissionDetailReviewDto
                    {
                        Id = evaluation.Id,
                        Sequence = index + 1,
                        Recommendation = string.IsNullOrWhiteSpace(evaluation.Recommendation) ? string.Empty : evaluation.Recommendation!,
                        Comment = evaluation.Comment,
                        TotalScore = evaluation.TotalScore,
                        ScoreCount = evaluationScores.Count,
                        CompletedAt = evaluation.CompletedAt,
                        CreatedDate = evaluation.CreatedDate,
                        Scores = evaluationScores
                            .Select(score => new SubmissionDetailReviewScoreDto
                            {
                                Id = score.Id,
                                CriterionName = score.CriterionName ?? score.CriterionCode ?? "-",
                                Score = score.Score,
                                Comment = score.Comment
                            })
                            .ToList()
                    };
                })
                .ToList();
        }

        private async Task<List<SubmissionDetailHistoryDto>> GetHistoriesAsync(
            Guid submissionId,
            Guid requestedLanguageId,
            Guid defaultLanguageId,
            CancellationToken cancellationToken)
        {
            List<HistoryProjection> rows = await _repository
                .Query()
                .AsNoTracking()
                .Where(submission => submission.Id == submissionId)
                .SelectMany(submission => submission.Histories)
                .Where(history => history.DeletedDate == null)
                .Where(history => history.FromStatusId.HasValue
                    || history.ToStatusId.HasValue
                    || (history.PublicNote != null && history.PublicNote != string.Empty))
                .OrderByDescending(history => history.PerformedAt)
                .ThenByDescending(history => history.CreatedDate)
                .Take(MaxHistoryRows)
                .Select(history => new HistoryProjection
                {
                    Id = history.Id,
                    FromStatusCode = history.FromStatus != null ? history.FromStatus.Code : string.Empty,
                    FromStatusName = history.FromStatus == null
                        ? "-"
                        : history.FromStatus.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == requestedLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault()
                          ?? history.FromStatus.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == defaultLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault()
                          ?? history.FromStatus.Code,
                    ToStatusCode = history.ToStatus != null ? history.ToStatus.Code : string.Empty,
                    ToStatusName = history.ToStatus == null
                        ? "-"
                        : history.ToStatus.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == requestedLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault()
                          ?? history.ToStatus.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == defaultLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault()
                          ?? history.ToStatus.Code,
                    TransitionName = history.TransactionStatusTransition == null
                        ? null
                        : history.TransactionStatusTransition.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == requestedLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault()
                          ?? history.TransactionStatusTransition.Translations
                            .Where(translation => translation.DeletedDate == null && translation.LanguageId == defaultLanguageId)
                            .Select(translation => translation.Name)
                            .FirstOrDefault(),
                    PublicNote = history.PublicNote,
                    Note = history.Note,
                    SourceAction = history.CreatedBy,
                    PerformedByName = history.PerformedByUser == null
                        ? null
                        : ((history.PerformedByUser.Name ?? string.Empty) + " " + (history.PerformedByUser.Surname ?? string.Empty)),
                    PerformedAt = history.PerformedAt,
                    IsAutomatic = history.IsAutomatic
                })
                .ToListAsync(cancellationToken);

            return rows
                .Select(history => new SubmissionDetailHistoryDto
                {
                    Id = history.Id,
                    FromStatusName = history.FromStatusName,
                    ToStatusName = history.ToStatusName,
                    FromStatusCode = history.FromStatusCode,
                    ToStatusCode = history.ToStatusCode,
                    DisplayTitle = ResolveHistoryDisplayTitle(history.TransitionName, history.ToStatusName, history.PublicNote),
                    DisplayDescription = ResolveHistoryDisplayDescription(history.PublicNote, history.FromStatusName, history.ToStatusName),
                    SourceAction = history.SourceAction,
                    PublicNote = history.PublicNote,
                    Note = history.Note,
                    PerformedByName = string.IsNullOrWhiteSpace(history.PerformedByName)
                        ? (history.IsAutomatic ? "Sistem" : "-")
                        : history.PerformedByName!,
                    PerformedAt = history.PerformedAt,
                    IsAutomatic = history.IsAutomatic
                })
                .ToList();
        }

        private async Task<List<SubmissionDetailFileDto>> GetFilesAsync(Guid submissionId, CancellationToken cancellationToken)
        {
            return await _repository
                .Query()
                .AsNoTracking()
                .Where(submission => submission.Id == submissionId)
                .SelectMany(submission => submission.Files)
                .Where(file => file.DeletedDate == null && file.IsActive)
                .OrderBy(file => file.FileKind)
                .ThenByDescending(file => file.CreatedDate)
                .Select(file => new SubmissionDetailFileDto
                {
                    Id = file.Id,
                    FileKind = file.FileKind,
                    FileKindText = file.FileKind.ToString(),
                    OriginalFileName = file.OriginalFileName,
                    FilePath = file.FilePath,
                    ContentType = file.ContentType,
                    FileSize = file.FileSize,
                    IsActive = file.IsActive,
                    UploadedAt = file.CreatedDate,
                    DisplayDate = file.CreatedDate,
                    DownloadByAcceptanceLetter = false
                })
                .ToListAsync(cancellationToken);
        }

        private async Task<List<AcceptanceLetterFileProjection>> GetLatestAcceptanceLettersAsync(Guid submissionId, CancellationToken cancellationToken)
        {
            List<AcceptanceLetterFileProjection> acceptanceLetters = await _acceptanceLetterRepository
                .Query()
                .AsNoTracking()
                .Where(letter => letter.SubmissionId == submissionId && letter.DeletedDate == null)
                .OrderByDescending(letter => letter.GeneratedAt)
                .Select(letter => new AcceptanceLetterFileProjection
                {
                    Id = letter.Id,
                    AuthorId = letter.AuthorId,
                    FileName = letter.FileName,
                    AuthorFullNameSnapshot = letter.AuthorFullNameSnapshot,
                    PdfObjectName = letter.PdfObjectName,
                    PdfFilePath = letter.PdfFilePath,
                    PdfContentType = letter.PdfContentType,
                    PdfFileSize = letter.PdfFileSize,
                    GeneratedAt = letter.GeneratedAt
                })
                .ToListAsync(cancellationToken);

            return acceptanceLetters
                .GroupBy(letter => letter.AuthorId ?? letter.Id)
                .Select(group => group.OrderByDescending(letter => letter.GeneratedAt).First())
                .OrderBy(letter => letter.AuthorFullNameSnapshot)
                .ToList();
        }

        private static List<SubmissionDetailFileDto> BuildFileDtos(
            List<SubmissionDetailFileDto> activeSubmissionFiles,
            List<AcceptanceLetterFileProjection> latestAcceptanceLetters)
        {
            List<SubmissionDetailFileDto> result = activeSubmissionFiles.ToList();

            HashSet<string> existingAcceptancePaths = result
                .Where(file => file.FileKind == SubmissionFileKind.AcceptanceLetter)
                .Select(file => NormalizePath(file.FilePath))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (AcceptanceLetterFileProjection letter in latestAcceptanceLetters)
            {
                string path = letter.PdfObjectName ?? letter.PdfFilePath ?? string.Empty;
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                if (existingAcceptancePaths.Contains(NormalizePath(path)))
                    continue;

                result.Add(new SubmissionDetailFileDto
                {
                    Id = letter.Id,
                    FileKind = SubmissionFileKind.AcceptanceLetter,
                    FileKindText = SubmissionFileKind.AcceptanceLetter.ToString(),
                    OriginalFileName = string.IsNullOrWhiteSpace(letter.FileName)
                        ? System.IO.Path.GetFileName(path)
                        : letter.FileName,
                    FilePath = path,
                    ContentType = string.IsNullOrWhiteSpace(letter.PdfContentType) ? "application/pdf" : letter.PdfContentType,
                    FileSize = letter.PdfFileSize,
                    IsActive = true,
                    UploadedAt = letter.GeneratedAt,
                    DisplayDate = letter.GeneratedAt,
                    DownloadByAcceptanceLetter = true
                });
            }

            return result
                .OrderBy(file => file.FileKind)
                .ThenByDescending(file => file.DisplayDate ?? file.UploadedAt)
                .ThenBy(file => file.OriginalFileName)
                .ToList();
        }

        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(
            string? culture,
            ApplicationLanguageDto defaultLanguage,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(culture))
                return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;

            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }

        private static string? ResolveTitleDisplayName(AuthorProjection author)
        {
            if (!string.IsNullOrWhiteSpace(author.RequestedTitleDescription))
                return author.RequestedTitleDescription!.Trim();

            if (!string.IsNullOrWhiteSpace(author.RequestedTitleName))
                return author.RequestedTitleName!.Trim();

            if (!string.IsNullOrWhiteSpace(author.DefaultTitleDescription))
                return author.DefaultTitleDescription!.Trim();

            if (!string.IsNullOrWhiteSpace(author.DefaultTitleName))
                return author.DefaultTitleName!.Trim();

            return author.TitleCode;
        }

        private static string? ResolveHistoryDisplayTitle(string? transitionName, string toStatusName, string? publicNote)
        {
            if (!string.IsNullOrWhiteSpace(transitionName))
                return transitionName;

            if (!string.IsNullOrWhiteSpace(toStatusName) && toStatusName != "-")
                return toStatusName;

            return !string.IsNullOrWhiteSpace(publicNote) ? "Güncelleme yapıldı" : null;
        }

        private static string? ResolveHistoryDisplayDescription(string? publicNote, string fromStatusName, string toStatusName)
        {
            if (!string.IsNullOrWhiteSpace(publicNote))
                return publicNote;

            bool hasFromStatus = !string.IsNullOrWhiteSpace(fromStatusName) && fromStatusName != "-";
            bool hasToStatus = !string.IsNullOrWhiteSpace(toStatusName) && toStatusName != "-";

            if (hasFromStatus && hasToStatus)
                return $"{fromStatusName} → {toStatusName}";

            if (hasToStatus)
                return toStatusName;

            return null;
        }

        private static bool IsDecisionCompleted(StatusProjection? status)
        {
            if (status is null)
                return false;

            string code = status.Code?.ToUpperInvariant() ?? string.Empty;

            return status.IsFinal == true
                || code is "ACCEPTED" or "PAYMENT_PENDING" or "COMPLETED" or "REJECTED" or "WITHDRAWN";
        }

        private static bool IsEditableByAuthor(StatusProjection? transactionStatus, bool isSubmitted)
        {
            if (transactionStatus is not null)
            {
                string statusCode = transactionStatus.Code?.Trim() ?? string.Empty;
                if (IsAuthorClosedStatusCode(statusCode))
                    return false;

                return transactionStatus.IsActive
                    && transactionStatus.DeletedDate == null
                    && transactionStatus.IsEditable == true;
            }

            return !isSubmitted;
        }

        private static bool IsAuthorClosedStatusCode(string statusCode)
        {
            return statusCode.Equals(SubmissionWorkflowStatusCodes.Submitted, StringComparison.OrdinalIgnoreCase)
                || statusCode.Equals(SubmissionWorkflowStatusCodes.ReviewerAssignment, StringComparison.OrdinalIgnoreCase)
                || statusCode.Equals(SubmissionWorkflowStatusCodes.UnderReview, StringComparison.OrdinalIgnoreCase)
                || statusCode.Equals(SubmissionWorkflowStatusCodes.ReviewsCompleted, StringComparison.OrdinalIgnoreCase)
                || statusCode.Equals(SubmissionWorkflowStatusCodes.EditorialDecision, StringComparison.OrdinalIgnoreCase)
                || statusCode.Equals(SubmissionWorkflowStatusCodes.Accepted, StringComparison.OrdinalIgnoreCase)
                || statusCode.Equals(SubmissionWorkflowStatusCodes.Rejected, StringComparison.OrdinalIgnoreCase)
                || statusCode.Equals(SubmissionWorkflowStatusCodes.PaymentPending, StringComparison.OrdinalIgnoreCase)
                || statusCode.Equals(SubmissionWorkflowStatusCodes.Completed, StringComparison.OrdinalIgnoreCase)
                || statusCode.Equals(SubmissionWorkflowStatusCodes.Withdrawn, StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolvePaymentBadgeClass(StatusProjection? paymentStatus, string displayName)
        {
            if (paymentStatus is null)
                return "bg-neutral-200 text-neutral-700";

            string value = string.Concat(paymentStatus.Code, " ", displayName).ToLowerInvariant();
            if (value.Contains("approved") || value.Contains("paid") || value.Contains("ödeme alındı") || value.Contains("ödeme işlemi yapıldı") || value.Contains("onay"))
                return "bg-success-100 text-success-600";

            if (value.Contains("reject") || value.Contains("red"))
                return "bg-danger-100 text-danger-600";

            return "bg-warning-100 text-warning-600";
        }

        private static string ResolveTransactionBadgeClass(StatusProjection? status, bool isSubmitted)
        {
            if (status is null)
                return isSubmitted ? "bg-info-100 text-info-600" : "bg-warning-100 text-warning-600";

            string value = status.Code.ToLowerInvariant();
            if (status.IsFinal == true && (value.Contains("accept") || value.Contains("kabul") || value.Contains("complete")))
                return "bg-success-100 text-success-600";

            if (status.IsFinal == true && (value.Contains("reject") || value.Contains("red")))
                return "bg-danger-100 text-danger-600";

            if (value.Contains("review") || value.Contains("hakem") || value.Contains("deger"))
                return "bg-info-100 text-info-600";

            return "bg-warning-100 text-warning-600";
        }

        private static string JoinFullName(string? firstName, string? lastName)
        {
            string fullName = $"{firstName} {lastName}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? "-" : fullName;
        }

        private static string NormalizePath(string? path)
            => string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim().Replace('\\', '/');

        private sealed class SubmissionHeaderProjection
        {
            public Guid Id { get; set; }
            public Guid CongressId { get; set; }
            public Guid? SubmissionTypeId { get; set; }
            public Guid? TopicId { get; set; }
            public Guid? CreatedByUserId { get; set; }
            public Guid? LanguageId { get; set; }
            public int? PaymentStatusId { get; set; }
            public int? TransactionStatusId { get; set; }
            public string SubmissionNumber { get; set; } = string.Empty;
            public string? Orcid { get; set; }
            public string Title { get; set; } = string.Empty;
            public string? TitleEn { get; set; }
            public string? Abstract { get; set; }
            public string? AbstractEn { get; set; }
            public string? Keywords { get; set; }
            public string? KeywordsEn { get; set; }
            public bool IsSubmitted { get; set; }
            public DateTime? SubmittedAt { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime? UpdatedDate { get; set; }
            public string CongressName { get; set; } = "-";
            public string? LanguageName { get; set; }
            public string? SubmissionTypeCode { get; set; }
            public string? SubmissionTypeName { get; set; }
            public SubmissionFormProfile FormProfile { get; set; } = SubmissionFormProfile.AcademicAbstract;
            public SubmissionDetailExhibitionDto? ExhibitionDetail { get; set; }
            public string? TopicCode { get; set; }
            public string? TopicName { get; set; }
            public StatusProjection? PaymentStatus { get; set; }
            public string? PaymentStatusName { get; set; }
            public StatusProjection? TransactionStatus { get; set; }
            public string? TransactionStatusName { get; set; }
            public int ReviewerCount { get; set; }
        }

        private sealed class StatusProjection
        {
            public int Id { get; set; }
            public string Code { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public bool? IsEditable { get; set; }
            public bool? IsFinal { get; set; }
            public DateTime? DeletedDate { get; set; }
        }

        private sealed class AuthorProjection
        {
            public Guid Id { get; set; }
            public Guid? TitleId { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Email { get; set; }
            public string? Institution { get; set; }
            public string? Orcid { get; set; }
            public bool IsCorrespondingAuthor { get; set; }
            public string? TitleCode { get; set; }
            public string? RequestedTitleDescription { get; set; }
            public string? RequestedTitleName { get; set; }
            public string? DefaultTitleDescription { get; set; }
            public string? DefaultTitleName { get; set; }
        }

        private sealed class EvaluationProjection
        {
            public Guid Id { get; set; }
            public string? Recommendation { get; set; }
            public string? Comment { get; set; }
            public decimal? TotalScore { get; set; }
            public DateTime? CompletedAt { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        private sealed class EvaluationScoreProjection
        {
            public Guid Id { get; set; }
            public Guid EvaluationId { get; set; }
            public string? CriterionCode { get; set; }
            public string? CriterionName { get; set; }
            public decimal Score { get; set; }
            public string? Comment { get; set; }
        }

        private sealed class HistoryProjection
        {
            public Guid Id { get; set; }
            public string FromStatusName { get; set; } = "-";
            public string ToStatusName { get; set; } = "-";
            public string FromStatusCode { get; set; } = string.Empty;
            public string ToStatusCode { get; set; } = string.Empty;
            public string? TransitionName { get; set; }
            public string? SourceAction { get; set; }
            public string? PublicNote { get; set; }
            public string? Note { get; set; }
            public string? PerformedByName { get; set; }
            public DateTime PerformedAt { get; set; }
            public bool IsAutomatic { get; set; }
        }

        private sealed class AcceptanceLetterFileProjection
        {
            public Guid Id { get; set; }
            public Guid? AuthorId { get; set; }
            public string FileName { get; set; } = string.Empty;
            public string AuthorFullNameSnapshot { get; set; } = string.Empty;
            public string? PdfObjectName { get; set; }
            public string? PdfFilePath { get; set; }
            public string? PdfContentType { get; set; }
            public long? PdfFileSize { get; set; }
            public DateTime GeneratedAt { get; set; }
        }
    }
}
