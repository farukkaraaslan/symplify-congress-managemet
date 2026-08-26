using Core.Application.Pipelines.Authorization;
using Core.Application.Pipelines.Caching;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.ReviewerEvaluations.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Application.Services.Workflow;
using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Features.ReviewerEvaluations.Commands.Save;

public sealed class SaveReviewerEvaluationCommand : IRequest<SavedReviewerEvaluationResponse>, ISecuredRequest, ICacheRemoverRequest
{
    public Guid EvaluationId { get; set; }
    public Guid? CurrentUserId { get; set; }
    public string? Recommendation { get; set; }
    public string? Comment { get; set; }
    public string? EditorComment { get; set; }
    public bool SubmitEvaluation { get; set; }
    public List<ReviewerEvaluationScoreInputDto> Scores { get; set; } = new();

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string CacheGroupKey => "GetReviewerEvaluations";

    public string[] Roles => new[]
    {
        ReviewerEvaluationsOperationClaims.Admin,
        ReviewerEvaluationsOperationClaims.Write,
        ReviewerEvaluationsOperationClaims.Save,
        ReviewerEvaluationsOperationClaims.Submit
    };

    public sealed class Handler : IRequestHandler<SaveReviewerEvaluationCommand, SavedReviewerEvaluationResponse>
    {
        private readonly ISubmissionEvaluationRepository _evaluationRepository;
        private readonly IEvaluationScoreRepository _scoreRepository;
        private readonly IEvaluationCriterionRepository _criterionRepository;
        private readonly IReviewerRepository _reviewerRepository;
        private readonly ISubmissionHistoryRepository _historyRepository;
        private readonly IWorkflowEngine _workflowEngine;

        public Handler(
            ISubmissionEvaluationRepository evaluationRepository,
            IEvaluationScoreRepository scoreRepository,
            IEvaluationCriterionRepository criterionRepository,
            IReviewerRepository reviewerRepository,
            ISubmissionHistoryRepository historyRepository,
            IWorkflowEngine workflowEngine)
        {
            _evaluationRepository = evaluationRepository;
            _scoreRepository = scoreRepository;
            _criterionRepository = criterionRepository;
            _reviewerRepository = reviewerRepository;
            _historyRepository = historyRepository;
            _workflowEngine = workflowEngine;
        }

        public async Task<SavedReviewerEvaluationResponse> Handle(SaveReviewerEvaluationCommand request, CancellationToken cancellationToken)
        {
            if (!request.CurrentUserId.HasValue || request.CurrentUserId.Value == Guid.Empty)
                throw new BusinessException(ReviewerEvaluationsMessages.ReviewerProfileNotFound);

            Reviewer? reviewer = await _reviewerRepository
                .Query()
                .FirstOrDefaultAsync(item =>
                    item.UserId == request.CurrentUserId.Value &&
                    item.IsActive &&
                    item.DeletedDate == null,
                    cancellationToken);

            if (reviewer is null)
                throw new BusinessException(ReviewerEvaluationsMessages.ReviewerProfileNotFound);

            SubmissionEvaluation? evaluation = await _evaluationRepository
                .Query()
                .Include(item => item.Submission)
                .Include(item => item.Scores)
                .FirstOrDefaultAsync(item => item.Id == request.EvaluationId && item.DeletedDate == null, cancellationToken);

            if (evaluation is null)
                throw new BusinessException(ReviewerEvaluationsMessages.EvaluationNotFound);

            if (evaluation.ReviewerId != reviewer.Id)
                throw new BusinessException(ReviewerEvaluationsMessages.EvaluationNotAssignedToCurrentReviewer);

            if (evaluation.CompletedAt.HasValue)
                throw new BusinessException(ReviewerEvaluationsMessages.EvaluationAlreadyCompleted);

            List<ReviewerEvaluationScoreInputDto> normalizedScores = request.Scores
                .Where(score => score.EvaluationCriterionId != Guid.Empty)
                .GroupBy(score => score.EvaluationCriterionId)
                .Select(group => group.Last())
                .ToList();

            request.SubmitEvaluation = true;

            if (normalizedScores.Count == 0 || normalizedScores.Any(score => !score.Score.HasValue))
                throw new BusinessException(ReviewerEvaluationsMessages.AllCriteriaMustBeScored);

            await ScoresShouldBeWithinCriterionMaximumAsync(normalizedScores, cancellationToken);

            if (string.IsNullOrWhiteSpace(request.Recommendation))
                throw new BusinessException(ReviewerEvaluationsMessages.RecommendationRequired);

            foreach (ReviewerEvaluationScoreInputDto scoreInput in normalizedScores)
            {
                EvaluationScore? existingScore = evaluation.Scores.FirstOrDefault(score =>
                    score.EvaluationCriterionId == scoreInput.EvaluationCriterionId &&
                    score.DeletedDate == null);

                if (existingScore is null)
                {
                    existingScore = new EvaluationScore
                    {
                        Id = Guid.NewGuid(),
                        SubmissionEvaluationId = evaluation.Id,
                        EvaluationCriterionId = scoreInput.EvaluationCriterionId,
                        Score = scoreInput.Score ?? 0,
                        Comment = Normalize(scoreInput.Comment)
                    };

                    await _scoreRepository.AddAsync(existingScore);
                    continue;
                }

                existingScore.Score = scoreInput.Score ?? 0;
                existingScore.Comment = Normalize(scoreInput.Comment);
                await _scoreRepository.UpdateAsync(existingScore);
            }

            decimal totalScore = normalizedScores
                .Where(score => score.Score.HasValue)
                .Sum(score => score.Score!.Value);

            evaluation.Comment = Normalize(request.Comment);
            evaluation.EditorComment = Normalize(request.EditorComment);
            evaluation.Recommendation = Normalize(request.Recommendation);
            evaluation.TotalScore = totalScore;
            evaluation.CompletedAt = DateTime.UtcNow;

            await _evaluationRepository.UpdateAsync(evaluation);

            await _historyRepository.AddAsync(new SubmissionHistory
            {
                Id = Guid.NewGuid(),
                SubmissionId = evaluation.SubmissionId,
                FromStatusId = null,
                ToStatusId = null,
                PerformedByUserId = request.CurrentUserId,
                Note = "Hakem değerlendirmesi tamamlandı.",
                InternalNote = evaluation.EditorComment,
                PublicNote = evaluation.Comment,
                PerformedAt = DateTime.UtcNow,
                IsAutomatic = false
            });

            if (IsAcceptanceRecommendation(evaluation.Recommendation) && await CanAutoAcceptSubmissionAsync(evaluation.SubmissionId, cancellationToken))
            {
                ChangeWorkflowStatusResult workflowResult = await _workflowEngine.ExecuteAutomaticTransitionToStatusAsync(
                    evaluation.SubmissionId,
                    "ACCEPTED",
                    request.CurrentUserId,
                    "Hakem değerlendirmesi olumlu tamamlandı. Kabul mektubu oluşturulacak.",
                    evaluation.EditorComment,
                    cancellationToken);

                if (!workflowResult.Success)
                    throw new BusinessException(workflowResult.Message ?? "Değerlendirme tamamlandı ancak bildiri kabul durumuna geçirilemedi.");
            }

            return new SavedReviewerEvaluationResponse
            {
                EvaluationId = evaluation.Id,
                SubmissionId = evaluation.SubmissionId,
                IsCompleted = evaluation.CompletedAt.HasValue,
                TotalScore = evaluation.TotalScore
            };
        }

        private async Task ScoresShouldBeWithinCriterionMaximumAsync(
            IReadOnlyCollection<ReviewerEvaluationScoreInputDto> scores,
            CancellationToken cancellationToken)
        {
            Guid[] criterionIds = scores
                .Select(score => score.EvaluationCriterionId)
                .Distinct()
                .ToArray();

            Dictionary<Guid, int> maxScores = await _criterionRepository
                .Query()
                .AsNoTracking()
                .Where(criterion => criterionIds.Contains(criterion.Id) && criterion.DeletedDate == null)
                .ToDictionaryAsync(
                    criterion => criterion.Id,
                    criterion => criterion.Score <= 0 ? 10 : criterion.Score,
                    cancellationToken);

            foreach (ReviewerEvaluationScoreInputDto score in scores)
            {
                if (!maxScores.TryGetValue(score.EvaluationCriterionId, out int maxScore))
                    throw new BusinessException(ReviewerEvaluationsMessages.AllCriteriaMustBeScored);

                decimal value = score.Score ?? -1;

                if (value < 0 || value > maxScore)
                    throw new BusinessException(ReviewerEvaluationsMessages.AllCriteriaMustBeScored);
            }
        }

        private async Task<bool> CanAutoAcceptSubmissionAsync(Guid submissionId, CancellationToken cancellationToken)
        {
            List<SubmissionEvaluation> evaluations = await _evaluationRepository
                .Query()
                .AsNoTracking()
                .Where(item => item.SubmissionId == submissionId && item.DeletedDate == null)
                .ToListAsync(cancellationToken);

            return evaluations.Count > 0 &&
                   evaluations.All(item => item.CompletedAt.HasValue) &&
                   evaluations.All(item => IsAcceptanceRecommendation(item.Recommendation));
        }

        private static bool IsAcceptanceRecommendation(string? recommendation)
        {
            if (string.IsNullOrWhiteSpace(recommendation))
                return false;

            string normalized = recommendation.Trim().ToUpperInvariant();
            return normalized is "KABUL" or "ACCEPT" or "ACCEPTED";
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
