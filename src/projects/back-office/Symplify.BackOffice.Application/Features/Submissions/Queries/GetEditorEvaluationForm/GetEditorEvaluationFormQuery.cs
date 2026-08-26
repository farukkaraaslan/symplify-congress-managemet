using Core.Application.Pipelines.Authorization;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.ReviewerEvaluations.Constants;
using Symplify.BackOffice.Application.Features.ReviewerEvaluations.Queries.GetForm;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Congress;
using Symplify.BackOffice.Domain.Enums;
using Symplify.BackOffice.Domain.Lookups;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Features.Submissions.Queries.GetEditorEvaluationForm;

public sealed class GetEditorEvaluationFormQuery : IRequest<GetReviewerEvaluationFormResponse>, ISecuredRequest
{
    public Guid SubmissionId { get; set; }
    public Guid? CurrentUserId { get; set; }
    public string? Culture { get; set; }

    public string[] Roles => new[]
    {
        SubmissionsOperationClaims.Admin,
        SubmissionsOperationClaims.Read,
        SubmissionsOperationClaims.Update,
        SubmissionsOperationClaims.Write
    };

    public sealed class Handler : IRequestHandler<GetEditorEvaluationFormQuery, GetReviewerEvaluationFormResponse>
    {
        private readonly ISubmissionRepository _submissionRepository;
        private readonly IReviewerRepository _reviewerRepository;
        private readonly ISubmissionEvaluationRepository _evaluationRepository;
        private readonly ICongressEvaluationCriterionRepository _congressEvaluationCriterionRepository;
        private readonly IEvaluationCriterionRepository _evaluationCriterionRepository;
        private readonly IApplicationLanguageProvider _languageProvider;

        public Handler(
            ISubmissionRepository submissionRepository,
            IReviewerRepository reviewerRepository,
            ISubmissionEvaluationRepository evaluationRepository,
            ICongressEvaluationCriterionRepository congressEvaluationCriterionRepository,
            IEvaluationCriterionRepository evaluationCriterionRepository,
            IApplicationLanguageProvider languageProvider)
        {
            _submissionRepository = submissionRepository;
            _reviewerRepository = reviewerRepository;
            _evaluationRepository = evaluationRepository;
            _congressEvaluationCriterionRepository = congressEvaluationCriterionRepository;
            _evaluationCriterionRepository = evaluationCriterionRepository;
            _languageProvider = languageProvider;
        }

        public async Task<GetReviewerEvaluationFormResponse> Handle(GetEditorEvaluationFormQuery request, CancellationToken cancellationToken)
        {
            if (!request.CurrentUserId.HasValue || request.CurrentUserId.Value == Guid.Empty)
                throw new BusinessException(SubmissionsMessages.UserInfoNotFound);

            Submission? submission = await _submissionRepository
                .Query()
                .AsNoTracking()
                .Include(item => item.Congress)
                .Include(item => item.SubmissionType)!
                    .ThenInclude(type => type.Translations)
                .Include(item => item.Topic)!
                    .ThenInclude(topic => topic.Translations)
                .Include(item => item.Files)
                .FirstOrDefaultAsync(item => item.Id == request.SubmissionId && item.DeletedDate == null, cancellationToken);

            if (submission is null)
                throw new BusinessException(SubmissionsMessages.EntityNotFound);

            Reviewer reviewer = await GetOrCreateEditorReviewerAsync(request.CurrentUserId.Value, cancellationToken);
            SubmissionEvaluation evaluation = await GetOrCreateEditorEvaluationAsync(submission.Id, reviewer.Id, cancellationToken);

            evaluation = await _evaluationRepository
                .Query()
                .AsNoTracking()
                .Include(item => item.Submission)
                    .ThenInclude(item => item.Congress)
                .Include(item => item.Submission)
                    .ThenInclude(item => item.SubmissionType)!
                        .ThenInclude(type => type.Translations)
                .Include(item => item.Submission)
                    .ThenInclude(item => item.Topic)!
                        .ThenInclude(topic => topic.Translations)
                .Include(item => item.Submission)
                    .ThenInclude(item => item.Files)
                .Include(item => item.Scores)
                    .ThenInclude(score => score.EvaluationCriterion)
                        .ThenInclude(criterion => criterion.Translations)
                .FirstAsync(item => item.Id == evaluation.Id, cancellationToken);

            Guid requestedLanguageId = await ResolveLanguageIdAsync(request.Culture, cancellationToken);
            Guid defaultLanguageId = await ResolveDefaultLanguageIdAsync(cancellationToken);
            List<ReviewerEvaluationCriterionDto> criteria = await ResolveCriteriaAsync(evaluation, requestedLanguageId, defaultLanguageId, cancellationToken);
            DateTime dueDate = ResolveDueDate(evaluation);
            bool isCompleted = evaluation.CompletedAt.HasValue;
            int daysRemaining = (int)Math.Ceiling((dueDate.Date - DateTime.UtcNow.Date).TotalDays);

            return new GetReviewerEvaluationFormResponse
            {
                EvaluationId = evaluation.Id,
                SubmissionId = evaluation.SubmissionId,
                SubmissionNumber = string.IsNullOrWhiteSpace(evaluation.Submission.SubmissionNumber) ? evaluation.Submission.Id.ToString("N")[..8].ToUpperInvariant() : evaluation.Submission.SubmissionNumber,
                CongressName = evaluation.Submission.Congress?.Name ?? "-",
                SubmissionTypeName = ResolveSubmissionTypeName(evaluation, requestedLanguageId, defaultLanguageId),
                TopicName = ResolveTopicName(evaluation, requestedLanguageId, defaultLanguageId),
                Title = evaluation.Submission.Title,
                TitleEn = evaluation.Submission.TitleEn,
                Abstract = evaluation.Submission.Abstract,
                AbstractEn = evaluation.Submission.AbstractEn,
                Keywords = evaluation.Submission.Keywords,
                KeywordsEn = evaluation.Submission.KeywordsEn,
                AssignedDate = evaluation.CreatedDate,
                DueDate = dueDate,
                IsCompleted = isCompleted,
                CompletedAt = evaluation.CompletedAt,
                StatusText = isCompleted ? ReviewerEvaluationResourceKeys.StatusCompleted : ReviewerEvaluationResourceKeys.StatusPending,
                StatusBadgeClass = isCompleted ? "bg-success-100 text-success-600" : daysRemaining < 0 ? "bg-danger-100 text-danger-600" : "bg-warning-100 text-warning-600",
                Recommendation = evaluation.Recommendation,
                Comment = evaluation.Comment,
                EditorComment = evaluation.EditorComment,
                TotalScore = evaluation.TotalScore,
                MaxScore = criteria.Sum(item => Math.Max(item.MaxScore, 0)),
                Criteria = criteria,
                Files = evaluation.Submission.Files
                    .Where(file => file.DeletedDate == null && file.IsActive)
                    .OrderByDescending(file => file.CreatedDate)
                    .Select(file => new ReviewerEvaluationFileDto
                    {
                        Id = file.Id,
                        FileName = string.IsNullOrWhiteSpace(file.OriginalFileName) ? "-" : file.OriginalFileName,
                        FileUrl = file.FilePath,
                        FileType = file.ContentType,
                        FileSize = file.FileSize,
                        CreatedDate = file.CreatedDate
                    })
                    .ToList()
            };
        }

        private async Task<Reviewer> GetOrCreateEditorReviewerAsync(Guid userId, CancellationToken cancellationToken)
        {
            Reviewer? reviewer = await _reviewerRepository
                .Query()
                .FirstOrDefaultAsync(item => item.UserId == userId && item.DeletedDate == null, cancellationToken);

            if (reviewer is not null)
            {
                if (!reviewer.IsActive || reviewer.Status == ReviewerStatus.Declined || reviewer.Status == ReviewerStatus.Pending)
                {
                    reviewer.IsActive = true;
                    reviewer.Status = ReviewerStatus.Accepted;
                    await _reviewerRepository.UpdateAsync(reviewer);
                }

                return reviewer;
            }

            reviewer = new Reviewer
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = ReviewerStatus.Accepted,
                IsActive = true
            };

            await _reviewerRepository.AddAsync(reviewer);
            return reviewer;
        }

        private async Task<SubmissionEvaluation> GetOrCreateEditorEvaluationAsync(Guid submissionId, Guid reviewerId, CancellationToken cancellationToken)
        {
            SubmissionEvaluation? evaluation = await _evaluationRepository
                .Query()
                .FirstOrDefaultAsync(item =>
                    item.SubmissionId == submissionId &&
                    item.ReviewerId == reviewerId &&
                    item.DeletedDate == null,
                    cancellationToken);

            if (evaluation is not null)
                return evaluation;

            evaluation = new SubmissionEvaluation
            {
                Id = Guid.NewGuid(),
                SubmissionId = submissionId,
                ReviewerId = reviewerId
            };

            await _evaluationRepository.AddAsync(evaluation);
            return evaluation;
        }

        private async Task<List<ReviewerEvaluationCriterionDto>> ResolveCriteriaAsync(
            SubmissionEvaluation evaluation,
            Guid requestedLanguageId,
            Guid defaultLanguageId,
            CancellationToken cancellationToken)
        {
            List<CongressEvaluationCriterion> congressCriteria = await _congressEvaluationCriterionRepository
                .Query()
                .AsNoTracking()
                .Include(item => item.EvaluationCriterion)
                    .ThenInclude(criterion => criterion.Translations)
                .Where(item =>
                    item.CongressId == evaluation.Submission.CongressId &&
                    item.IsActive &&
                    item.DeletedDate == null &&
                    item.EvaluationCriterion.IsActive &&
                    item.EvaluationCriterion.DeletedDate == null)
                .OrderBy(item => item.Order)
                .ToListAsync(cancellationToken);

            List<EvaluationCriterion> criteria = congressCriteria
                .Select(item => item.EvaluationCriterion)
                .ToList();

            if (criteria.Count == 0)
            {
                criteria = await _evaluationCriterionRepository
                    .Query()
                    .AsNoTracking()
                    .Include(item => item.Translations)
                    .Where(item => item.IsActive && item.DeletedDate == null)
                    .OrderBy(item => item.Order)
                    .ToListAsync(cancellationToken);
            }

            List<EvaluationScore> existingScores = evaluation.Scores
                .Where(score => score.DeletedDate == null)
                .ToList();

            return criteria.Select((criterion, index) =>
            {
                EvaluationScore? score = existingScores.FirstOrDefault(item => item.EvaluationCriterionId == criterion.Id);
                var translations = criterion.Translations.Where(item => item.DeletedDate == null).ToList();

                return new ReviewerEvaluationCriterionDto
                {
                    EvaluationCriterionId = criterion.Id,
                    Name = translations.FirstOrDefault(item => item.LanguageId == requestedLanguageId)?.Name
                        ?? translations.FirstOrDefault(item => item.LanguageId == defaultLanguageId)?.Name
                        ?? criterion.Code
                        ?? $"Criterion {index + 1}",
                    Description = translations.FirstOrDefault(item => item.LanguageId == requestedLanguageId)?.Description
                        ?? translations.FirstOrDefault(item => item.LanguageId == defaultLanguageId)?.Description,
                    Order = index + 1,
                    MaxScore = criterion.Score <= 0 ? 10 : criterion.Score,
                    Score = score?.Score,
                    Comment = score?.Comment
                };
            }).ToList();
        }

        private async Task<Guid> ResolveLanguageIdAsync(string? culture, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(culture))
            {
                var language = await _languageProvider.GetByCultureAsync(culture, cancellationToken);
                if (language is not null)
                    return language.Id;
            }

            return await ResolveDefaultLanguageIdAsync(cancellationToken);
        }

        private async Task<Guid> ResolveDefaultLanguageIdAsync(CancellationToken cancellationToken)
        {
            var defaultLanguage = await _languageProvider.GetDefaultLanguageAsync(cancellationToken);
            return defaultLanguage.Id;
        }

        private static string ResolveSubmissionTypeName(SubmissionEvaluation evaluation, Guid requestedLanguageId, Guid defaultLanguageId)
        {
            var translations = evaluation.Submission.SubmissionType?.Translations?.Where(item => item.DeletedDate == null).ToList() ?? new();
            return translations.FirstOrDefault(item => item.LanguageId == requestedLanguageId)?.Name
                ?? translations.FirstOrDefault(item => item.LanguageId == defaultLanguageId)?.Name
                ?? evaluation.Submission.SubmissionType?.Code
                ?? "-";
        }

        private static string ResolveTopicName(SubmissionEvaluation evaluation, Guid requestedLanguageId, Guid defaultLanguageId)
        {
            var translations = evaluation.Submission.Topic?.Translations?.Where(item => item.DeletedDate == null).ToList() ?? new();
            return translations.FirstOrDefault(item => item.LanguageId == requestedLanguageId)?.Name
                ?? translations.FirstOrDefault(item => item.LanguageId == defaultLanguageId)?.Name
                ?? evaluation.Submission.Topic?.Code
                ?? "-";
        }

        private static DateTime ResolveDueDate(SubmissionEvaluation evaluation)
        {
            return evaluation.CreatedDate == default
                ? DateTime.UtcNow.Date.AddDays(7)
                : evaluation.CreatedDate.Date.AddDays(7);
        }
    }
}
