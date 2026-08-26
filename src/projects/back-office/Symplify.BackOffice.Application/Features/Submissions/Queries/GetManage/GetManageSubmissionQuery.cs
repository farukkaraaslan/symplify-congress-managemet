using Core.Application.Pipelines.Authorization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Common.Localization;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Features.Submissions.Rules;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.Workflow;
using Symplify.BackOffice.Domain.Communication;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Domain.Workflow;
using PaymentDocumentEntity = Symplify.BackOffice.Domain.Workflow.PaymentDocument;
using SubmissionFileKindEnum = Symplify.BackOffice.Domain.Enums.SubmissionFileKind;

namespace Symplify.BackOffice.Application.Features.Submissions.Queries.GetManage;

public sealed class GetManageSubmissionQuery : IRequest<GetManageSubmissionResponse>, ISecuredRequest
{
    public Guid Id { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public Guid? LanguageId { get; set; }
    public string? Culture { get; set; }

    public string[] Roles => new[]
    {
        SubmissionsOperationClaims.Admin,
        SubmissionsOperationClaims.Read
    };

    public sealed class Handler : IRequestHandler<GetManageSubmissionQuery, GetManageSubmissionResponse>
    {
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IReviewerRepository _reviewerRepository;
        private readonly ICongressReviewerRepository _congressReviewerRepository;
        private readonly ISubmissionTypeRepository _submissionTypeRepository;
        private readonly ISubmissionTypeTranslationRepository _submissionTypeTranslationRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly ITopicTranslationRepository _topicTranslationRepository;
        private readonly IPaymentStatusRepository _paymentStatusRepository;
        private readonly IPaymentStatusTranslationRepository _paymentStatusTranslationRepository;
        private readonly ITransactionStatusRepository _transactionStatusRepository;
        private readonly ITransactionStatusTranslationRepository _transactionStatusTranslationRepository;
        private readonly ITransactionStatusTransitionRepository _transactionStatusTransitionRepository;
        private readonly ISubmissionHistoryRepository _historyRepository;
        private readonly ISubmissionFileRepository _fileRepository;
        private readonly IPaymentDocumentRepository _paymentDocumentRepository;
        private readonly ISubmissionAcceptanceLetterRepository _acceptanceLetterRepository;
        private readonly IMailOutboxMessageRepository _mailOutboxMessageRepository;
        private readonly IWorkflowEngine _workflowEngine;
        private readonly IApplicationLanguageProvider _languageProvider;
        private readonly ICurrentLanguageProvider _currentLanguageProvider;
        private readonly ITranslationFallbackResolver _fallbackResolver;
        private readonly SubmissionBusinessRules _rules;

        public Handler(
            ISubmissionRepository submissionRepository,
            IReviewerRepository reviewerRepository,
            ICongressReviewerRepository congressReviewerRepository,
            ISubmissionTypeRepository submissionTypeRepository,
            ISubmissionTypeTranslationRepository submissionTypeTranslationRepository,
            ITopicRepository topicRepository,
            ITopicTranslationRepository topicTranslationRepository,
            IPaymentStatusRepository paymentStatusRepository,
            IPaymentStatusTranslationRepository paymentStatusTranslationRepository,
            ITransactionStatusRepository transactionStatusRepository,
            ITransactionStatusTranslationRepository transactionStatusTranslationRepository,
            ITransactionStatusTransitionRepository transactionStatusTransitionRepository,
            ISubmissionHistoryRepository historyRepository,
            ISubmissionFileRepository fileRepository,
            IPaymentDocumentRepository paymentDocumentRepository,
            ISubmissionAcceptanceLetterRepository acceptanceLetterRepository,
            IMailOutboxMessageRepository mailOutboxMessageRepository,
            IWorkflowEngine workflowEngine,
            IApplicationLanguageProvider languageProvider,
            ICurrentLanguageProvider currentLanguageProvider,
            ITranslationFallbackResolver fallbackResolver,
            SubmissionBusinessRules rules)
        {
            _submissionRepository = submissionRepository;
            _reviewerRepository = reviewerRepository;
            _congressReviewerRepository = congressReviewerRepository;
            _submissionTypeRepository = submissionTypeRepository;
            _submissionTypeTranslationRepository = submissionTypeTranslationRepository;
            _topicRepository = topicRepository;
            _topicTranslationRepository = topicTranslationRepository;
            _paymentStatusRepository = paymentStatusRepository;
            _paymentStatusTranslationRepository = paymentStatusTranslationRepository;
            _transactionStatusRepository = transactionStatusRepository;
            _transactionStatusTranslationRepository = transactionStatusTranslationRepository;
            _transactionStatusTransitionRepository = transactionStatusTransitionRepository;
            _historyRepository = historyRepository;
            _fileRepository = fileRepository;
            _paymentDocumentRepository = paymentDocumentRepository;
            _acceptanceLetterRepository = acceptanceLetterRepository;
            _mailOutboxMessageRepository = mailOutboxMessageRepository;
            _workflowEngine = workflowEngine;
            _languageProvider = languageProvider;
            _currentLanguageProvider = currentLanguageProvider;
            _fallbackResolver = fallbackResolver;
            _rules = rules;
        }

        public async Task<GetManageSubmissionResponse> Handle(GetManageSubmissionQuery request, CancellationToken cancellationToken)
        {
            ApplicationLanguageDto defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            ApplicationLanguageDto requestedLanguage = await ResolveRequestedLanguageAsync(request.LanguageId, request.Culture, defaultLanguage, cancellationToken);

            Submission? submission = await _submissionRepository
                .Query()
                .AsNoTracking()
                .Include(item => item.Congress)
                    .ThenInclude(congress => congress.Translations)
                .Include(item => item.Authors)
                    .ThenInclude(author => author.Title)
                        .ThenInclude(title => title!.Translations)
                .Include(item => item.ExhibitionDetail)
                .Include(item => item.Reviewers)
                    .ThenInclude(reviewer => reviewer.User)
                .Include(item => item.Evaluations)
                    .ThenInclude(evaluation => evaluation.Reviewer)
                        .ThenInclude(reviewer => reviewer.User)
                .Include(item => item.Evaluations)
                    .ThenInclude(evaluation => evaluation.Scores)
                        .ThenInclude(score => score.EvaluationCriterion)
                            .ThenInclude(criterion => criterion.Translations)
                .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

            await _rules.SubmissionShouldExistWhenSelected(submission);
            submission = submission!;

            SubmissionType? submissionType = submission.SubmissionTypeId.HasValue
                ? await _submissionTypeRepository.Query().AsNoTracking().FirstOrDefaultAsync(item => item.Id == submission.SubmissionTypeId.Value, cancellationToken)
                : null;

            Topic? topic = submission.TopicId.HasValue
                ? await _topicRepository.Query().AsNoTracking().FirstOrDefaultAsync(item => item.Id == submission.TopicId.Value, cancellationToken)
                : null;

            PaymentStatus? paymentStatus = submission.PaymentStatusId.HasValue
                ? await _paymentStatusRepository.Query().AsNoTracking().FirstOrDefaultAsync(item => item.Id == submission.PaymentStatusId.Value, cancellationToken)
                : null;

            TransactionStatus? transactionStatus = submission.TransactionStatusId.HasValue
                ? await _transactionStatusRepository.Query().AsNoTracking().FirstOrDefaultAsync(item => item.Id == submission.TransactionStatusId.Value, cancellationToken)
                : null;

            List<SubmissionTypeTranslation> submissionTypeTranslations = submissionType is null
                ? new List<SubmissionTypeTranslation>()
                : await _submissionTypeTranslationRepository.Query().AsNoTracking().Where(item => item.SubmissionTypeId == submissionType.Id).ToListAsync(cancellationToken);

            List<TopicTranslation> topicTranslations = topic is null
                ? new List<TopicTranslation>()
                : await _topicTranslationRepository.Query().AsNoTracking().Where(item => item.TopicId == topic.Id).ToListAsync(cancellationToken);

            List<PaymentStatusTranslation> paymentStatusTranslations = paymentStatus is null
                ? new List<PaymentStatusTranslation>()
                : await _paymentStatusTranslationRepository.Query().AsNoTracking().Where(item => item.PaymentStatusId == paymentStatus.Id).ToListAsync(cancellationToken);

            List<TransactionStatusTranslation> transactionStatusTranslations = transactionStatus is null
                ? new List<TransactionStatusTranslation>()
                : await _transactionStatusTranslationRepository.Query().AsNoTracking().Where(item => item.TransactionStatusId == transactionStatus.Id).ToListAsync(cancellationToken);

            IReadOnlyCollection<AllowedWorkflowTransitionDto> allowedTransitions = await _workflowEngine.GetAllowedTransitionsAsync(
                submission.Id,
                request.PerformedByUserId,
                cancellationToken);

            List<AllowedWorkflowTransitionDto> localizedAllowedTransitions = await LocalizeAllowedTransitionsAsync(
                allowedTransitions,
                requestedLanguage.Id,
                defaultLanguage.Id,
                cancellationToken);

            List<SubmissionHistory> histories = await _historyRepository
                .Query()
                .AsNoTracking()
                .Include(history => history.FromStatus)
                    .ThenInclude(status => status!.Translations)
                .Include(history => history.ToStatus)
                    .ThenInclude(status => status!.Translations)
                .Include(history => history.PerformedByUser)
                .Where(history => history.SubmissionId == submission.Id)
                .OrderByDescending(history => history.PerformedAt)
                .ThenByDescending(history => history.CreatedDate)
                .ToListAsync(cancellationToken);

            List<SubmissionFile> files = await _fileRepository
                .Query()
                .AsNoTracking()
                .Where(file => file.SubmissionId == submission.Id && file.IsActive && file.DeletedDate == null)
                .OrderBy(file => file.FileKind)
                .ThenByDescending(file => file.CreatedDate)
                .ToListAsync(cancellationToken);

            List<PaymentDocumentEntity> paymentDocuments = await _paymentDocumentRepository
                .Query()
                .AsNoTracking()
                .Where(document => document.SubmissionId == submission.Id && document.DeletedDate == null)
                .OrderByDescending(document => document.CreatedDate)
                .ToListAsync(cancellationToken);

            List<SubmissionAcceptanceLetter> acceptanceLetters = await _acceptanceLetterRepository
                .Query()
                .AsNoTracking()
                .Where(letter => letter.SubmissionId == submission.Id && letter.DeletedDate == null)
                .OrderByDescending(letter => letter.GeneratedAt)
                .ToListAsync(cancellationToken);

            List<MailOutboxMessage> mailMessages = await _mailOutboxMessageRepository
                .Query()
                .AsNoTracking()
                .Where(message => message.RelatedSubmissionId == submission.Id && message.DeletedDate == null)
                .OrderByDescending(message => message.CreatedDate)
                .ToListAsync(cancellationToken);

            List<Author> activeAuthors = submission.Authors.Where(author => author.DeletedDate == null).ToList();
            List<SubmissionEvaluation> evaluations = submission.Evaluations.Where(evaluation => evaluation.DeletedDate == null).ToList();
            List<Reviewer> reviewers = submission.Reviewers.Where(reviewer => reviewer.DeletedDate == null).ToList();
            List<Guid> assignedReviewerIds = reviewers.Select(reviewer => reviewer.Id).ToList();

            List<CongressReviewer> congressReviewerCandidates = await _congressReviewerRepository
                .Query()
                .AsNoTracking()
                .Include(item => item.Reviewer)
                    .ThenInclude(reviewer => reviewer.User)
                .Where(item => item.CongressId == submission.CongressId
                    && item.IsActive
                    && item.DeletedDate == null
                    && item.Reviewer.DeletedDate == null
                    && item.Reviewer.IsActive
                    && !assignedReviewerIds.Contains(item.ReviewerId))
                .OrderBy(item => item.Reviewer.User.Name)
                .ThenBy(item => item.Reviewer.User.Surname)
                .ToListAsync(cancellationToken);

            List<Guid> congressPoolReviewerIds = congressReviewerCandidates.Select(item => item.ReviewerId).ToList();

            List<Reviewer> globalReviewerCandidates = await _reviewerRepository
                .Query()
                .AsNoTracking()
                .Include(item => item.User)
                .Where(item => item.DeletedDate == null
                    && item.IsActive
                    && !assignedReviewerIds.Contains(item.Id)
                    && !congressPoolReviewerIds.Contains(item.Id))
                .OrderBy(item => item.User.Name)
                .ThenBy(item => item.User.Surname)
                .Take(100)
                .ToListAsync(cancellationToken);

            string submissionTypeName = ResolveName(submissionTypeTranslations, requestedLanguage.Id, defaultLanguage.Id) ?? submissionType?.Code ?? "-";
            string topicName = ResolveName(topicTranslations, requestedLanguage.Id, defaultLanguage.Id) ?? topic?.Code ?? "-";
            string paymentStatusName = ResolveName(paymentStatusTranslations, requestedLanguage.Id, defaultLanguage.Id) ?? paymentStatus?.Code ?? "-";
            string transactionStatusName = ResolveName(transactionStatusTranslations, requestedLanguage.Id, defaultLanguage.Id)
                ?? transactionStatus?.Code
                ?? (submission.IsSubmitted ? "Onaya Gönderildi" : "Taslak");

            string congressName = ResolveCongressTitle(submission.Congress, requestedLanguage.Id, defaultLanguage.Id);

            decimal? averageScore = evaluations
                .Where(evaluation => evaluation.TotalScore.HasValue)
                .Select(evaluation => evaluation.TotalScore!.Value)
                .DefaultIfEmpty()
                .Average();

            if (averageScore == 0 && !evaluations.Any(evaluation => evaluation.TotalScore.HasValue))
                averageScore = null;

            return new GetManageSubmissionResponse
            {
                Id = submission.Id,
                CongressId = submission.CongressId,
                CongressName = congressName,
                CongressCode = submission.Congress?.Code,
                SubmissionTypeId = submission.SubmissionTypeId,
                TopicId = submission.TopicId,
                CreatedByUserId = submission.CreatedByUserId,
                LanguageId = submission.LanguageId,
                PaymentStatusId = submission.PaymentStatusId,
                TransactionStatusId = submission.TransactionStatusId,
                TransactionStatusCode = transactionStatus?.Code,
                PaymentStatusCode = paymentStatus?.Code,
                SubmissionNumber = submission.SubmissionNumber,
                Orcid = submission.Orcid,
                Title = submission.Title,
                TitleEn = submission.TitleEn,
                Abstract = submission.Abstract,
                AbstractEn = submission.AbstractEn,
                Keywords = submission.Keywords,
                KeywordsEn = submission.KeywordsEn,
                IsSubmitted = submission.IsSubmitted,
                SubmittedAt = submission.SubmittedAt,
                CreatedDate = submission.CreatedDate,
                UpdatedDate = submission.UpdatedDate,
                SubmissionTypeName = submissionTypeName,
                TopicName = topicName,
                PaymentStatusName = paymentStatusName,
                TransactionStatusName = transactionStatusName,
                PaymentStatusBadgeClass = ResolvePaymentBadgeClass(paymentStatus, paymentStatusName),
                TransactionStatusBadgeClass = ResolveTransactionBadgeClass(transactionStatus, submission.IsSubmitted),
                AverageScore = averageScore,
                IsExhibitionApplication = submission.ExhibitionDetail is not null,
                ExhibitionDetail = submission.ExhibitionDetail is null
                    ? null
                    : new ManageSubmissionExhibitionDetailDto
                    {
                        WorkName = submission.ExhibitionDetail.WorkName,
                        Dimensions = submission.ExhibitionDetail.Dimensions,
                        Technique = submission.ExhibitionDetail.Technique,
                        Description = submission.ExhibitionDetail.Description,
                        Address = submission.ExhibitionDetail.Address
                    },
                Authors = activeAuthors
                    .OrderByDescending(author => author.IsCorrespondingAuthor)
                    .ThenBy(author => JoinFullName(author.FirstName, author.LastName))
                    .Select(author => new ManageSubmissionAuthorDto
                    {
                        Id = author.Id,
                        TitleId = author.TitleId,
                        TitleName = ResolveTitleName(author.Title?.Translations, requestedLanguage.Id, defaultLanguage.Id) ?? author.Title?.Code,
                        FullName = JoinFullName(author.FirstName, author.LastName),
                        Email = author.Email,
                        Institution = author.Institution,
                        Orcid = author.Orcid,
                        IsCorrespondingAuthor = author.IsCorrespondingAuthor
                    })
                    .ToList(),
                Reviewers = reviewers
                    .OrderBy(reviewer => reviewer.User.Name)
                    .ThenBy(reviewer => reviewer.User.Surname)
                    .Select(reviewer => new ManageSubmissionReviewerDto
                    {
                        Id = reviewer.Id,
                        FullName = JoinFullName(reviewer.User.Name, reviewer.User.Surname),
                        Email = reviewer.User.Email,
                        Institution = reviewer.User.Institution,
                        Orcid = reviewer.User.Orcid,
                        Status = reviewer.Status.ToString(),
                        AssignedAt = reviewer.CreatedDate,
                        Evaluation = MapEvaluation(evaluations.FirstOrDefault(evaluation => evaluation.ReviewerId == reviewer.Id), requestedLanguage.Id, defaultLanguage.Id)
                    })
                    .ToList(),
                ReviewerCandidates = congressReviewerCandidates
                    .Select(candidate => new ManageSubmissionReviewerCandidateDto
                    {
                        ReviewerId = candidate.ReviewerId,
                        FullName = JoinFullName(candidate.Reviewer.User.Name, candidate.Reviewer.User.Surname),
                        Email = candidate.Reviewer.User.Email,
                        Institution = candidate.Reviewer.User.Institution,
                        Orcid = candidate.Reviewer.User.Orcid,
                        IsInCongressPool = true
                    })
                    .Concat(globalReviewerCandidates.Select(candidate => new ManageSubmissionReviewerCandidateDto
                    {
                        ReviewerId = candidate.Id,
                        FullName = JoinFullName(candidate.User.Name, candidate.User.Surname),
                        Email = candidate.User.Email,
                        Institution = candidate.User.Institution,
                        Orcid = candidate.User.Orcid,
                        IsInCongressPool = false
                    }))
                    .OrderBy(candidate => candidate.FullName)
                    .ToList(),
                Evaluations = evaluations
                    .OrderByDescending(evaluation => evaluation.CompletedAt ?? evaluation.UpdatedDate ?? evaluation.CreatedDate)
                    .Select(evaluation => MapEvaluation(evaluation, requestedLanguage.Id, defaultLanguage.Id))
                    .ToList(),
                Histories = histories.Select(history => new ManageSubmissionHistoryDto
                    {
                        Id = history.Id,
                        FromStatusName = ResolveStatusName(history.FromStatus, requestedLanguage.Id, defaultLanguage.Id),
                        ToStatusName = ResolveStatusName(history.ToStatus, requestedLanguage.Id, defaultLanguage.Id),
                        FromStatusCode = history.FromStatus?.Code ?? string.Empty,
                        ToStatusCode = history.ToStatus?.Code ?? string.Empty,
                        SourceAction = history.CreatedBy,
                        Note = history.Note,
                        PublicNote = history.PublicNote,
                        InternalNote = history.InternalNote,
                        PerformedByName = history.PerformedByUser is null
                            ? (history.IsAutomatic ? "Sistem" : "-")
                            : JoinFullName(history.PerformedByUser.Name, history.PerformedByUser.Surname),
                        PerformedAt = history.PerformedAt,
                        IsAutomatic = history.IsAutomatic
                    })
                    .ToList(),
                Files = NormalizeFileList(files)
                    .Select(file => new ManageSubmissionFileDto
                    {
                        Id = file.Id,
                        FileKind = file.FileKind.ToString(),
                        DisplayKind = file.FileKind switch
                        {
                            SubmissionFileKindEnum.AcceptanceLetter => "Kabul Belgesi",
                            SubmissionFileKindEnum.ParticipationCertificate => "Katılım Belgesi",
                            _ => file.FileKind.ToString()
                        },
                        IsAcceptanceLetter = file.FileKind == SubmissionFileKindEnum.AcceptanceLetter,
                        OriginalFileName = file.OriginalFileName,
                        FilePath = file.FilePath,
                        ContentType = file.ContentType,
                        FileSize = file.FileSize,
                        UploadedAt = file.CreatedDate,
                        DisplayDate = file.CreatedDate
                    })
                    .ToList(),
                PaymentDocuments = paymentDocuments.Select(document => new ManageSubmissionPaymentDocumentDto
                    {
                        Id = document.Id,
                        OriginalFileName = document.OriginalFileName,
                        FilePath = document.FilePath,
                        ContentType = document.ContentType,
                        Size = document.Size,
                        IsApproved = document.IsApproved,
                        UploadedAt = document.CreatedDate
                    })
                    .ToList(),
                AcceptanceLetters = acceptanceLetters.Select(letter => new ManageSubmissionAcceptanceLetterDto
                    {
                        Id = letter.Id,
                        LetterNumber = letter.LetterNumber,
                        PdfFilePath = letter.PdfFilePath,
                        GeneratedAt = letter.GeneratedAt,
                        SentAt = letter.SentAt,
                        SentToEmail = letter.SentToEmail
                    })
                    .ToList(),
                MailMessages = mailMessages.Select(message => new ManageSubmissionMailMessageDto
                    {
                        Id = message.Id,
                        ToEmail = message.ToEmail,
                        Subject = message.Subject,
                        Status = message.Status.ToString(),
                        AttemptCount = message.AttemptCount,
                        SentAt = message.SentAt,
                        LastAttemptAt = message.LastAttemptAt,
                        LastError = message.LastError
                    })
                    .ToList(),
                WorkflowTransitions = localizedAllowedTransitions
            };
        }

        private async Task<List<AllowedWorkflowTransitionDto>> LocalizeAllowedTransitionsAsync(
            IReadOnlyCollection<AllowedWorkflowTransitionDto> transitions,
            Guid requestedLanguageId,
            Guid defaultLanguageId,
            CancellationToken cancellationToken)
        {
            if (!transitions.Any())
                return new List<AllowedWorkflowTransitionDto>();

            List<int> transitionIds = transitions
                .Select(transition => transition.TransitionId)
                .Distinct()
                .ToList();

            List<TransactionStatusTransition> transitionEntities = await _transactionStatusTransitionRepository
                .Query()
                .AsNoTracking()
                .Include(transition => transition.Translations)
                .Include(transition => transition.ToStatus)
                    .ThenInclude(status => status.Translations)
                .Where(transition => transitionIds.Contains(transition.Id))
                .ToListAsync(cancellationToken);

            Dictionary<int, TransactionStatusTransition> transitionMap = transitionEntities.ToDictionary(transition => transition.Id);

            foreach (AllowedWorkflowTransitionDto transition in transitions)
            {
                if (!transitionMap.TryGetValue(transition.TransitionId, out TransactionStatusTransition? transitionEntity))
                    continue;

                string transitionName = ResolveName(transitionEntity.Translations, requestedLanguageId, defaultLanguageId) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(transitionName))
                    transition.DisplayText = transitionName;

                transition.ToStatusCode = transitionEntity.ToStatus.Code;
                transition.BadgeClass = ResolveTransactionBadgeClass(transitionEntity.ToStatus, isSubmitted: true);
            }

            return transitions.ToList();
        }

        private async Task<ApplicationLanguageDto> ResolveRequestedLanguageAsync(
            Guid? languageId,
            string? culture,
            ApplicationLanguageDto defaultLanguage,
            CancellationToken cancellationToken)
        {
            if (languageId.HasValue && languageId.Value != Guid.Empty)
                return await _languageProvider.GetByIdAsync(languageId.Value, cancellationToken) ?? defaultLanguage;

            if (!string.IsNullOrWhiteSpace(culture))
                return await _languageProvider.GetByCultureAsync(culture, cancellationToken) ?? defaultLanguage;

            return await _currentLanguageProvider.GetCurrentLanguageAsync(cancellationToken);
        }

        private string? ResolveName<TTranslation>(IEnumerable<TTranslation>? translations, Guid requestedLanguageId, Guid defaultLanguageId)
            where TTranslation : class
        {
            if (translations is null)
                return null;

            TTranslation? displayTranslation = _fallbackResolver.Resolve(
                translations.Where(translation => !IsDeleted(translation)).ToList(),
                requestedLanguageId,
                defaultLanguageId);

            object? value = displayTranslation?.GetType().GetProperty("Name")?.GetValue(displayTranslation);
            return value?.ToString();
        }

        private string ResolveStatusName(TransactionStatus? status, Guid requestedLanguageId, Guid defaultLanguageId)
        {
            if (status is null)
                return "-";

            string? name = ResolveName(status.Translations, requestedLanguageId, defaultLanguageId);
            return string.IsNullOrWhiteSpace(name) ? status.Code : name;
        }

        private string ResolveCongressTitle(Congress congress, Guid requestedLanguageId, Guid defaultLanguageId)
        {
            CongressTranslation? translation = _fallbackResolver.Resolve(
                congress.Translations.Where(item => item.DeletedDate == null).ToList(),
                requestedLanguageId,
                defaultLanguageId);

            if (!string.IsNullOrWhiteSpace(translation?.Title))
                return translation.Title;

            if (!string.IsNullOrWhiteSpace(congress.Name))
                return congress.Name;

            return !string.IsNullOrWhiteSpace(congress.Code) ? congress.Code : congress.Id.ToString();
        }

        private ManageSubmissionEvaluationDto? MapEvaluation(SubmissionEvaluation? evaluation, Guid requestedLanguageId, Guid defaultLanguageId)
        {
            if (evaluation is null)
                return null;

            List<ManageSubmissionEvaluationScoreDto> scores = evaluation.Scores
                .Where(score => score.DeletedDate == null)
                .OrderBy(score => score.EvaluationCriterion.Order)
                .ThenBy(score => score.EvaluationCriterion.Code)
                .Select(score => new ManageSubmissionEvaluationScoreDto
                {
                    Id = score.Id,
                    CriterionName = ResolveName(score.EvaluationCriterion.Translations, requestedLanguageId, defaultLanguageId)
                        ?? score.EvaluationCriterion.Code
                        ?? "-",
                    Score = score.Score,
                    Comment = score.Comment
                })
                .ToList();

            return new ManageSubmissionEvaluationDto
            {
                Id = evaluation.Id,
                ReviewerId = evaluation.ReviewerId,
                ReviewerName = evaluation.Reviewer is null ? "-" : JoinFullName(evaluation.Reviewer.User.Name, evaluation.Reviewer.User.Surname),
                Comment = evaluation.Comment,
                Recommendation = evaluation.Recommendation,
                TotalScore = evaluation.TotalScore,
                CompletedAt = evaluation.CompletedAt,
                CreatedDate = evaluation.CreatedDate,
                ScoreCount = scores.Count,
                Scores = scores
            };
        }

        private static List<SubmissionFile> NormalizeFileList(IEnumerable<SubmissionFile> files)
        {
            List<SubmissionFile> activeFiles = files
                .Where(file => file.DeletedDate == null && file.IsActive)
                .ToList();

            List<SubmissionFile> nonAcceptanceFiles = activeFiles
                .Where(file => file.FileKind != SubmissionFileKindEnum.AcceptanceLetter)
                .OrderBy(file => file.FileKind)
                .ThenByDescending(file => file.CreatedDate)
                .ToList();

            List<SubmissionFile> currentAcceptanceFiles = activeFiles
                .Where(file => file.FileKind == SubmissionFileKindEnum.AcceptanceLetter)
                .GroupBy(file => string.IsNullOrWhiteSpace(file.OriginalFileName)
                    ? file.FilePath
                    : file.OriginalFileName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderByDescending(file => file.CreatedDate)
                    .ThenByDescending(file => file.Id)
                    .First())
                .OrderByDescending(file => file.CreatedDate)
                .ToList();

            return currentAcceptanceFiles
                .Concat(nonAcceptanceFiles)
                .ToList();
        }

        private string? ResolveTitleName<TTranslation>(IEnumerable<TTranslation>? translations, Guid requestedLanguageId, Guid defaultLanguageId)
            where TTranslation : class
        {
            if (translations is null)
                return null;

            TTranslation? displayTranslation = _fallbackResolver.Resolve(
                translations.Where(translation => !IsDeleted(translation)).ToList(),
                requestedLanguageId,
                defaultLanguageId);

            object? description = displayTranslation?.GetType().GetProperty("Description")?.GetValue(displayTranslation);
            if (!string.IsNullOrWhiteSpace(description?.ToString()))
                return description.ToString();

            object? name = displayTranslation?.GetType().GetProperty("Name")?.GetValue(displayTranslation);
            return name?.ToString();
        }

        private static string ResolvePaymentBadgeClass(PaymentStatus? paymentStatus, string displayName)
        {
            if (paymentStatus is null)
                return "bg-neutral-200 text-neutral-700";

            string value = string.Concat(paymentStatus.Code, " ", displayName).ToLowerInvariant();
            if (value.Contains("approved") || value.Contains("paid") || value.Contains("ödeme alındı") || value.Contains("onay"))
                return "bg-success-100 text-success-600";

            if (value.Contains("reject") || value.Contains("red"))
                return "bg-danger-100 text-danger-600";

            return "bg-warning-100 text-warning-600";
        }

        private static string ResolveTransactionBadgeClass(TransactionStatus? status, bool isSubmitted)
        {
            if (status is null)
                return isSubmitted ? "bg-info-100 text-info-600" : "bg-warning-100 text-warning-600";

            string value = status.Code.ToLowerInvariant();
            if (status.IsFinal && (value.Contains("accept") || value.Contains("kabul")))
                return "bg-success-100 text-success-600";

            if (status.IsFinal && (value.Contains("reject") || value.Contains("red")))
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

        private static bool IsDeleted(object entity)
        {
            object? value = LocalizedEntityRuntimeHelper.GetPropertyValue(entity, "DeletedDate");
            return value is not null;
        }
    }
}

public sealed class GetManageSubmissionResponse
{
    public Guid Id { get; set; }
    public Guid CongressId { get; set; }
    public string CongressName { get; set; } = "-";
    public string? CongressCode { get; set; }
    public Guid? SubmissionTypeId { get; set; }
    public Guid? TopicId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? LanguageId { get; set; }
    public int? PaymentStatusId { get; set; }
    public int? TransactionStatusId { get; set; }
    public string? TransactionStatusCode { get; set; }
    public string? PaymentStatusCode { get; set; }
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
    public string SubmissionTypeName { get; set; } = "-";
    public string TopicName { get; set; } = "-";
    public string PaymentStatusName { get; set; } = "-";
    public string TransactionStatusName { get; set; } = "Taslak";
    public string PaymentStatusBadgeClass { get; set; } = "bg-neutral-200 text-neutral-700";
    public string TransactionStatusBadgeClass { get; set; } = "bg-neutral-200 text-neutral-700";
    public decimal? AverageScore { get; set; }
    public bool IsExhibitionApplication { get; set; }
    public ManageSubmissionExhibitionDetailDto? ExhibitionDetail { get; set; }
    public List<ManageSubmissionAuthorDto> Authors { get; set; } = new();
    public List<ManageSubmissionReviewerDto> Reviewers { get; set; } = new();
    public List<ManageSubmissionReviewerCandidateDto> ReviewerCandidates { get; set; } = new();
    public List<ManageSubmissionEvaluationDto> Evaluations { get; set; } = new();
    public List<ManageSubmissionHistoryDto> Histories { get; set; } = new();
    public List<ManageSubmissionFileDto> Files { get; set; } = new();
    public List<ManageSubmissionPaymentDocumentDto> PaymentDocuments { get; set; } = new();
    public List<ManageSubmissionAcceptanceLetterDto> AcceptanceLetters { get; set; } = new();
    public List<ManageSubmissionMailMessageDto> MailMessages { get; set; } = new();
    public List<AllowedWorkflowTransitionDto> WorkflowTransitions { get; set; } = new();
}

public sealed class ManageSubmissionExhibitionDetailDto
{
    public string WorkName { get; set; } = string.Empty;
    public string? Dimensions { get; set; }
    public string Technique { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
}

public sealed class ManageSubmissionAuthorDto
{
    public Guid Id { get; set; }
    public Guid? TitleId { get; set; }
    public string? TitleName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Institution { get; set; }
    public string? Orcid { get; set; }
    public bool IsCorrespondingAuthor { get; set; }
}

public sealed class ManageSubmissionReviewerDto
{
    public Guid Id { get; set; }
    public Guid? TitleId { get; set; }
    public string? TitleName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Institution { get; set; }
    public string? Orcid { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public ManageSubmissionEvaluationDto? Evaluation { get; set; }
}

public sealed class ManageSubmissionReviewerCandidateDto
{
    public Guid ReviewerId { get; set; }
    public Guid? TitleId { get; set; }
    public string? TitleName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Institution { get; set; }
    public string? Orcid { get; set; }
    public bool IsInCongressPool { get; set; }
}

public sealed class ManageSubmissionEvaluationDto
{
    public Guid Id { get; set; }
    public Guid ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string? Recommendation { get; set; }
    public decimal? TotalScore { get; set; }
    public int ScoreCount { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<ManageSubmissionEvaluationScoreDto> Scores { get; set; } = new();
}

public sealed class ManageSubmissionEvaluationScoreDto
{
    public Guid Id { get; set; }
    public string CriterionName { get; set; } = "-";
    public decimal Score { get; set; }
    public string? Comment { get; set; }
}

public sealed class ManageSubmissionHistoryDto
{
    public Guid Id { get; set; }
    public string FromStatusName { get; set; } = "-";
    public string ToStatusName { get; set; } = "-";
    public string FromStatusCode { get; set; } = string.Empty;
    public string ToStatusCode { get; set; } = string.Empty;
    public string? SourceAction { get; set; }
    public string? Note { get; set; }
    public string? PublicNote { get; set; }
    public string? InternalNote { get; set; }
    public string PerformedByName { get; set; } = "-";
    public DateTime PerformedAt { get; set; }
    public bool IsAutomatic { get; set; }
}

public sealed class ManageSubmissionFileDto
{
    public Guid Id { get; set; }
    public string FileKind { get; set; } = string.Empty;
    public string DisplayKind { get; set; } = string.Empty;
    public bool IsAcceptanceLetter { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? DisplayDate { get; set; }
}

public sealed class ManageSubmissionPaymentDocumentDto
{
    public Guid Id { get; set; }
    public string? OriginalFileName { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? Size { get; set; }
    public bool IsApproved { get; set; }
    public DateTime UploadedAt { get; set; }
}

public sealed class ManageSubmissionAcceptanceLetterDto
{
    public Guid Id { get; set; }
    public string LetterNumber { get; set; } = string.Empty;
    public string? PdfFilePath { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? SentToEmail { get; set; }
}

public sealed class ManageSubmissionMailMessageDto
{
    public Guid Id { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? LastError { get; set; }
}
