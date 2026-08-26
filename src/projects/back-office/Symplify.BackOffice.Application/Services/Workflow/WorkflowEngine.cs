using Core.Persistence.Paging;
using Microsoft.EntityFrameworkCore;
using Symplify.BackOffice.Application.Features.ReviewerEvaluations.Constants;
using Symplify.BackOffice.Application.Services.Repositories;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Services.Workflow;

public sealed class WorkflowEngine : IWorkflowEngine
{
    private const string AutomaticTransitionCannotBeTriggeredManuallyMessage =
        "Bu geçiş sistem tarafından otomatik çalıştırılır. Kullanıcı ekranından manuel tetiklenemez.";

    private readonly ISubmissionRepository _submissionRepository;
    private readonly ISubmissionEvaluationRepository _submissionEvaluationRepository;
    private readonly IPaymentStatusRepository _paymentStatusRepository;
    private readonly ITransactionStatusRepository _transactionStatusRepository;
    private readonly ITransactionStatusTransitionRepository _transitionRepository;
    private readonly IWorkflowTransitionConditionRepository _conditionRepository;
    private readonly IWorkflowTransitionEffectRepository _effectRepository;
    private readonly ISubmissionHistoryRepository _historyRepository;
    private readonly IWorkflowConditionEvaluator _conditionEvaluator;
    private readonly IWorkflowEffectProcessor _effectProcessor;

    public WorkflowEngine(
        ISubmissionRepository submissionRepository,
        ISubmissionEvaluationRepository submissionEvaluationRepository,
        IPaymentStatusRepository paymentStatusRepository,
        ITransactionStatusRepository transactionStatusRepository,
        ITransactionStatusTransitionRepository transitionRepository,
        IWorkflowTransitionConditionRepository conditionRepository,
        IWorkflowTransitionEffectRepository effectRepository,
        ISubmissionHistoryRepository historyRepository,
        IWorkflowConditionEvaluator conditionEvaluator,
        IWorkflowEffectProcessor effectProcessor)
    {
        _submissionRepository = submissionRepository;
        _submissionEvaluationRepository = submissionEvaluationRepository;
        _paymentStatusRepository = paymentStatusRepository;
        _transactionStatusRepository = transactionStatusRepository;
        _transitionRepository = transitionRepository;
        _conditionRepository = conditionRepository;
        _effectRepository = effectRepository;
        _historyRepository = historyRepository;
        _conditionEvaluator = conditionEvaluator;
        _effectProcessor = effectProcessor;
    }

    public async Task<IReadOnlyCollection<AllowedWorkflowTransitionDto>> GetAllowedTransitionsAsync(
        Guid submissionId,
        Guid? performedByUserId,
        CancellationToken cancellationToken)
    {
        Submission? submission = await _submissionRepository.GetAsync(
            predicate: item => item.Id == submissionId,
            cancellationToken: cancellationToken);

        if (submission?.TransactionStatusId is null)
            return Array.Empty<AllowedWorkflowTransitionDto>();

        IPaginate<TransactionStatusTransition> transitions = await _transitionRepository.GetListAsync(
            predicate: transition =>
                transition.FromStatusId == submission.TransactionStatusId &&
                transition.IsActive &&
                !transition.IsAuto,
            index: 0,
            size: 100,
            cancellationToken: cancellationToken);

        List<AllowedWorkflowTransitionDto> result = new();

        foreach (TransactionStatusTransition transition in transitions.Items.OrderBy(item => item.Id))
        {
            TransactionStatus? toStatus = await _transactionStatusRepository.GetAsync(
                predicate: status => status.Id == transition.ToStatusId,
                cancellationToken: cancellationToken);

            IReadOnlyCollection<WorkflowTransitionCondition> conditions = await GetConditionsAsync(transition.Id, cancellationToken);
            WorkflowContext context = new()
            {
                Submission = submission,
                Transition = transition,
                PerformedByUserId = performedByUserId
            };

            WorkflowConditionEvaluationResult conditionResult = await _conditionEvaluator.EvaluateAsync(context, conditions, cancellationToken);
            WorkflowGuardResult guardResult = await EvaluateManualWorkflowGuardAsync(submission.Id, cancellationToken);

            bool isAllowed = conditionResult.IsAllowed && guardResult.IsAllowed;
            string? disabledReason = !guardResult.IsAllowed
                ? guardResult.Message
                : conditionResult.FailureMessage;

            result.Add(new AllowedWorkflowTransitionDto
            {
                TransitionId = transition.Id,
                FromStatusId = transition.FromStatusId,
                ToStatusId = transition.ToStatusId,
                ToStatusCode = toStatus?.Code ?? transition.ToStatusId.ToString(),
                DisplayText = toStatus?.Code ?? $"Status {transition.ToStatusId}",
                BadgeClass = "bg-primary-50 text-primary-600",
                IsAllowed = isAllowed,
                DisabledReason = disabledReason,
                IsAuto = false
            });
        }

        return result;
    }

    public async Task<ChangeWorkflowStatusResult> ChangeStatusAsync(
        Guid submissionId,
        int transitionId,
        Guid? performedByUserId,
        string? publicNote,
        string? internalNote,
        CancellationToken cancellationToken)
    {
        Submission? submission = await _submissionRepository.GetAsync(
            predicate: item => item.Id == submissionId,
            cancellationToken: cancellationToken);

        if (submission is null)
            return ChangeWorkflowStatusResult.Failed("Submission not found.");

        TransactionStatusTransition? transition = await _transitionRepository.GetAsync(
            predicate: item => item.Id == transitionId && item.IsActive,
            cancellationToken: cancellationToken);

        if (transition is null)
            return ChangeWorkflowStatusResult.Failed("Workflow transition not found.");

        if (transition.IsAuto)
            return ChangeWorkflowStatusResult.Failed(AutomaticTransitionCannotBeTriggeredManuallyMessage);

        if (submission.TransactionStatusId != transition.FromStatusId)
            return ChangeWorkflowStatusResult.Failed("Workflow transition is not valid for current status.");

        WorkflowGuardResult guardResult = await EvaluateManualWorkflowGuardAsync(submission.Id, cancellationToken);
        if (!guardResult.IsAllowed)
            return ChangeWorkflowStatusResult.Failed(guardResult.Message ?? ReviewerEvaluationsMessages.PendingReviewerEvaluationsBlockEditor);

        IReadOnlyCollection<WorkflowTransitionCondition> conditions = await GetConditionsAsync(transition.Id, cancellationToken);
        WorkflowContext context = new()
        {
            Submission = submission,
            Transition = transition,
            PerformedByUserId = performedByUserId,
            PublicNote = publicNote,
            InternalNote = internalNote
        };

        WorkflowConditionEvaluationResult conditionResult = await _conditionEvaluator.EvaluateAsync(context, conditions, cancellationToken);
        if (!conditionResult.IsAllowed)
            return ChangeWorkflowStatusResult.Failed(conditionResult.FailureMessage ?? "Workflow condition failed.");

        return await ApplyTransitionAsync(
            submission,
            transition,
            performedByUserId,
            publicNote,
            internalNote,
            isAutomatic: false,
            cancellationToken);
    }

    public async Task<ChangeWorkflowStatusResult> ChangeStatusByCodeAsync(
        Guid submissionId,
        string targetStatusCode,
        Guid? performedByUserId,
        string? publicNote,
        string? internalNote,
        CancellationToken cancellationToken)
    {
        string? normalizedTargetStatusCode = ResolveDirectStatusCode(targetStatusCode);
        if (string.IsNullOrWhiteSpace(normalizedTargetStatusCode))
            return ChangeWorkflowStatusResult.Failed("Bu işlem için geçerli hedef durum bulunamadı.");

        Submission? submission = await _submissionRepository.GetAsync(
            predicate: item => item.Id == submissionId,
            cancellationToken: cancellationToken);

        if (submission is null)
            return ChangeWorkflowStatusResult.Failed("Submission not found.");

        if (submission.TransactionStatusId is null)
            return ChangeWorkflowStatusResult.Failed("Submission has no current workflow status.");

        TransactionStatus? currentStatus = await _transactionStatusRepository.GetAsync(
            predicate: status => status.Id == submission.TransactionStatusId.Value,
            cancellationToken: cancellationToken);

        TransactionStatus? targetStatus = await _transactionStatusRepository.GetAsync(
            predicate: status =>
                status.Code == normalizedTargetStatusCode &&
                status.DeletedDate == null &&
                status.IsActive,
            cancellationToken: cancellationToken);

        if (targetStatus is null)
            return ChangeWorkflowStatusResult.Failed($"Hedef workflow durumu bulunamadı. Kod: {normalizedTargetStatusCode}");

        if (submission.TransactionStatusId == targetStatus.Id)
            return ChangeWorkflowStatusResult.Ok(submission.TransactionStatusId);

        if (IsWorkflowCode(normalizedTargetStatusCode, SubmissionWorkflowStatusCodes.Submitted) &&
            !IsWorkflowCode(currentStatus?.Code, SubmissionWorkflowStatusCodes.Rejected))
        {
            return ChangeWorkflowStatusResult.Failed("Sadece reddedilmiş bildiri tekrar gönderildi durumuna alınabilir.");
        }

        if (IsWorkflowCode(normalizedTargetStatusCode, SubmissionWorkflowStatusCodes.Rejected) &&
            IsWorkflowCode(currentStatus?.Code, SubmissionWorkflowStatusCodes.Rejected))
        {
            return ChangeWorkflowStatusResult.Failed("Bildiri zaten reddedilmiş durumda.");
        }

        return await ApplyDirectStatusChangeAsync(
            submission,
            targetStatus,
            performedByUserId,
            publicNote,
            internalNote,
            cancellationToken);
    }

    public async Task<ChangeWorkflowStatusResult> ExecuteNextAutomaticTransitionAsync(
        Guid submissionId,
        Guid? performedByUserId,
        string? publicNote,
        string? internalNote,
        CancellationToken cancellationToken)
    {
        Submission? submission = await _submissionRepository.GetAsync(
            predicate: item => item.Id == submissionId,
            cancellationToken: cancellationToken);

        if (submission is null)
            return ChangeWorkflowStatusResult.Failed("Submission not found.");

        if (submission.TransactionStatusId is null)
            return ChangeWorkflowStatusResult.Failed("Submission has no current workflow status.");

        IPaginate<TransactionStatusTransition> transitions = await _transitionRepository.GetListAsync(
            predicate: transition =>
                transition.FromStatusId == submission.TransactionStatusId.Value &&
                transition.IsActive &&
                transition.IsAuto,
            index: 0,
            size: 20,
            cancellationToken: cancellationToken);

        foreach (TransactionStatusTransition transition in transitions.Items.OrderBy(item => item.Id))
        {
            IReadOnlyCollection<WorkflowTransitionCondition> conditions = await GetConditionsAsync(transition.Id, cancellationToken);
            WorkflowContext context = new()
            {
                Submission = submission,
                Transition = transition,
                PerformedByUserId = performedByUserId,
                PublicNote = publicNote,
                InternalNote = internalNote
            };

            WorkflowConditionEvaluationResult conditionResult = await _conditionEvaluator.EvaluateAsync(context, conditions, cancellationToken);
            if (!conditionResult.IsAllowed)
                continue;

            return await ApplyTransitionAsync(
                submission,
                transition,
                performedByUserId,
                publicNote,
                internalNote,
                isAutomatic: true,
                cancellationToken);
        }

        return ChangeWorkflowStatusResult.Failed("Uygun otomatik workflow geçişi bulunamadı.");
    }


    public async Task<ChangeWorkflowStatusResult> ExecuteAutomaticTransitionToStatusAsync(
        Guid submissionId,
        string targetStatusCode,
        Guid? performedByUserId,
        string? publicNote,
        string? internalNote,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetStatusCode))
            return ChangeWorkflowStatusResult.Failed("Target workflow status code is required.");

        Submission? submission = await _submissionRepository.GetAsync(
            predicate: item => item.Id == submissionId,
            cancellationToken: cancellationToken);

        if (submission is null)
            return ChangeWorkflowStatusResult.Failed("Submission not found.");

        if (submission.TransactionStatusId is null)
            return ChangeWorkflowStatusResult.Failed("Submission has no current workflow status.");

        TransactionStatus? targetStatus = await _transactionStatusRepository.GetAsync(
            predicate: status =>
                status.Code == targetStatusCode &&
                status.DeletedDate == null,
            cancellationToken: cancellationToken);

        if (targetStatus is null)
            return ChangeWorkflowStatusResult.Failed($"Target workflow status was not found. Code: {targetStatusCode}");

        if (submission.TransactionStatusId == targetStatus.Id)
            return ChangeWorkflowStatusResult.Ok(submission.TransactionStatusId);

        IPaginate<TransactionStatusTransition> transitions = await _transitionRepository.GetListAsync(
            predicate: transition =>
                transition.FromStatusId == submission.TransactionStatusId.Value &&
                transition.ToStatusId == targetStatus.Id &&
                transition.IsActive &&
                transition.DeletedDate == null,
            index: 0,
            size: 20,
            cancellationToken: cancellationToken);

        foreach (TransactionStatusTransition transition in transitions.Items
                     .OrderByDescending(item => item.IsAuto)
                     .ThenBy(item => item.Id))
        {
            IReadOnlyCollection<WorkflowTransitionCondition> conditions = await GetConditionsAsync(transition.Id, cancellationToken);
            WorkflowContext context = new()
            {
                Submission = submission,
                Transition = transition,
                PerformedByUserId = performedByUserId,
                PublicNote = publicNote,
                InternalNote = internalNote
            };

            WorkflowConditionEvaluationResult conditionResult = await _conditionEvaluator.EvaluateAsync(context, conditions, cancellationToken);
            if (!conditionResult.IsAllowed)
                continue;

            return await ApplyTransitionAsync(
                submission,
                transition,
                performedByUserId,
                publicNote,
                internalNote,
                isAutomatic: true,
                cancellationToken);
        }

        return ChangeWorkflowStatusResult.Failed($"Uygun otomatik workflow geçişi bulunamadı. Hedef durum: {targetStatusCode}.");
    }

    private async Task<ChangeWorkflowStatusResult> ApplyTransitionAsync(
        Submission submission,
        TransactionStatusTransition transition,
        Guid? performedByUserId,
        string? publicNote,
        string? internalNote,
        bool isAutomatic,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<WorkflowTransitionEffect> effects = await GetEffectsAsync(transition.Id, cancellationToken);

        WorkflowContext context = new()
        {
            Submission = submission,
            Transition = transition,
            PerformedByUserId = performedByUserId,
            PublicNote = publicNote,
            InternalNote = internalNote
        };

        int? previousStatusId = submission.TransactionStatusId;
        submission.TransactionStatusId = transition.ToStatusId;

        await ApplyAcceptedPaymentDefaultsAsync(submission, transition.ToStatusId, cancellationToken);

        await _submissionRepository.UpdateAsync(submission);

        SubmissionHistory history = new()
        {
            Id = Guid.NewGuid(),
            SubmissionId = submission.Id,
            FromStatusId = previousStatusId,
            ToStatusId = transition.ToStatusId,
            TransactionStatusTransitionId = transition.Id,
            PerformedByUserId = performedByUserId,
            PublicNote = publicNote,
            InternalNote = internalNote,
            Note = publicNote,
            PerformedAt = DateTime.UtcNow,
            IsAutomatic = isAutomatic
        };

        await _historyRepository.AddAsync(history);
        await _effectProcessor.ProcessAsync(context, effects, cancellationToken);

        return ChangeWorkflowStatusResult.Ok(submission.TransactionStatusId);
    }

    private async Task<ChangeWorkflowStatusResult> ApplyDirectStatusChangeAsync(
        Submission submission,
        TransactionStatus targetStatus,
        Guid? performedByUserId,
        string? publicNote,
        string? internalNote,
        CancellationToken cancellationToken)
    {
        int? previousStatusId = submission.TransactionStatusId;
        submission.TransactionStatusId = targetStatus.Id;

        if (IsWorkflowCode(targetStatus.Code, SubmissionWorkflowStatusCodes.Submitted))
        {
            submission.IsSubmitted = true;
            submission.SubmittedAt ??= DateTime.UtcNow;
        }

        submission.UpdatedDate = DateTime.UtcNow;
        submission.UpdatedBy = "WorkflowStatusByCode";

        await _submissionRepository.UpdateAsync(submission);

        string defaultNote = IsWorkflowCode(targetStatus.Code, SubmissionWorkflowStatusCodes.Rejected)
            ? "Bildiri editör tarafından reddedildi."
            : "Bildiri tekrar gönderildi durumuna alındı.";

        SubmissionHistory history = new()
        {
            Id = Guid.NewGuid(),
            SubmissionId = submission.Id,
            FromStatusId = previousStatusId,
            ToStatusId = targetStatus.Id,
            TransactionStatusTransitionId = null,
            PerformedByUserId = performedByUserId,
            PublicNote = publicNote,
            InternalNote = internalNote,
            Note = string.IsNullOrWhiteSpace(publicNote) ? defaultNote : publicNote,
            PerformedAt = DateTime.UtcNow,
            IsAutomatic = false,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "WorkflowStatusByCode"
        };

        await _historyRepository.AddAsync(history);

        return ChangeWorkflowStatusResult.Ok(submission.TransactionStatusId);
    }

    private async Task ApplyAcceptedPaymentDefaultsAsync(
        Submission submission,
        int toStatusId,
        CancellationToken cancellationToken)
    {
        TransactionStatus? toStatus = await _transactionStatusRepository.GetAsync(
            predicate: status => status.Id == toStatusId,
            cancellationToken: cancellationToken);

        if (!IsWorkflowCode(toStatus?.Code, "ACCEPTED"))
            return;

        if (await IsPaidPaymentStatusAsync(submission.PaymentStatusId, cancellationToken))
            return;

        int? pendingPaymentStatusId = await ResolvePaymentStatusIdAsync(
            cancellationToken,
            "PAYMENT_PENDING",
            "PENDING",
            "WAITING_PAYMENT");

        if (pendingPaymentStatusId.HasValue)
            submission.PaymentStatusId = pendingPaymentStatusId.Value;
    }

    private async Task<bool> IsPaidPaymentStatusAsync(int? paymentStatusId, CancellationToken cancellationToken)
    {
        if (!paymentStatusId.HasValue)
            return false;

        PaymentStatus? paymentStatus = await _paymentStatusRepository.GetAsync(
            predicate: status => status.Id == paymentStatusId.Value,
            cancellationToken: cancellationToken);

        return IsWorkflowCode(
            paymentStatus?.Code,
            "PAID",
            "PAYMENT_PAID",
            "PAYMENT_COMPLETED",
            "COMPLETED",
            "APPROVED",
            "PAYMENT_APPROVED");
    }

    private async Task<int?> ResolvePaymentStatusIdAsync(CancellationToken cancellationToken, params string[] codes)
    {
        foreach (string code in codes.Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            string normalizedCode = code.Trim();
            PaymentStatus? paymentStatus = await _paymentStatusRepository.GetAsync(
                predicate: status =>
                    status.DeletedDate == null &&
                    status.IsActive &&
                    status.Code == normalizedCode,
                cancellationToken: cancellationToken);

            if (paymentStatus is not null)
                return paymentStatus.Id;
        }

        return null;
    }

    private static string? ResolveDirectStatusCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string normalized = NormalizeWorkflowCode(value);
        if (normalized == NormalizeWorkflowCode(SubmissionWorkflowStatusCodes.Rejected))
            return SubmissionWorkflowStatusCodes.Rejected;

        if (normalized == NormalizeWorkflowCode(SubmissionWorkflowStatusCodes.Submitted))
            return SubmissionWorkflowStatusCodes.Submitted;

        return null;
    }

    private static bool IsWorkflowCode(string? value, params string[] expectedCodes)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string normalized = NormalizeWorkflowCode(value);
        return expectedCodes.Any(expected => NormalizeWorkflowCode(expected) == normalized);
    }

    private static string NormalizeWorkflowCode(string value)
        => new(value.Trim().Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private async Task<WorkflowGuardResult> EvaluateManualWorkflowGuardAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        bool hasPendingReviewerEvaluations = await _submissionEvaluationRepository
            .Query()
            .AnyAsync(evaluation =>
                evaluation.SubmissionId == submissionId &&
                evaluation.DeletedDate == null &&
                evaluation.CompletedAt == null,
                cancellationToken);

        return hasPendingReviewerEvaluations
            ? WorkflowGuardResult.Blocked(ReviewerEvaluationsMessages.PendingReviewerEvaluationsBlockEditor)
            : WorkflowGuardResult.Allowed();
    }

    private async Task<IReadOnlyCollection<WorkflowTransitionCondition>> GetConditionsAsync(int transitionId, CancellationToken cancellationToken)
    {
        IPaginate<WorkflowTransitionCondition> conditions = await _conditionRepository.GetListAsync(
            predicate: condition => condition.TransactionStatusTransitionId == transitionId && condition.IsActive,
            orderBy: query => query.OrderBy(condition => condition.Order),
            index: 0,
            size: 100,
            cancellationToken: cancellationToken);

        return conditions.Items.ToArray();
    }

    private async Task<IReadOnlyCollection<WorkflowTransitionEffect>> GetEffectsAsync(int transitionId, CancellationToken cancellationToken)
    {
        IPaginate<WorkflowTransitionEffect> effects = await _effectRepository.GetListAsync(
            predicate: effect => effect.TransactionStatusTransitionId == transitionId && effect.IsActive,
            orderBy: query => query.OrderBy(effect => effect.Order),
            index: 0,
            size: 100,
            cancellationToken: cancellationToken);

        return effects.Items.ToArray();
    }

    private sealed record WorkflowGuardResult(bool IsAllowed, string? Message)
    {
        public static WorkflowGuardResult Allowed() => new(true, null);
        public static WorkflowGuardResult Blocked(string message) => new(false, message);
    }
}
