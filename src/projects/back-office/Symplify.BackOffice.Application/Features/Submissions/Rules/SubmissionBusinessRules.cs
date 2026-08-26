using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.Types;
using Symplify.BackOffice.Application.Features.Submissions.Constants;
using Symplify.BackOffice.Application.Services.Workflow;
using Symplify.BackOffice.Domain.Submission;
using Symplify.BackOffice.Domain.Workflow;

namespace Symplify.BackOffice.Application.Features.Submissions.Rules;

public class SubmissionBusinessRules : BaseBusinessRules
{
    private static readonly HashSet<string> AuthorClosedStatusCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        SubmissionWorkflowStatusCodes.Submitted,
        SubmissionWorkflowStatusCodes.ReviewerAssignment,
        SubmissionWorkflowStatusCodes.UnderReview,
        SubmissionWorkflowStatusCodes.ReviewsCompleted,
        SubmissionWorkflowStatusCodes.EditorialDecision,
        SubmissionWorkflowStatusCodes.Accepted,
        SubmissionWorkflowStatusCodes.Rejected,
        SubmissionWorkflowStatusCodes.PaymentPending,
        SubmissionWorkflowStatusCodes.Completed,
        SubmissionWorkflowStatusCodes.Withdrawn
    };

    public Task SubmissionShouldExistWhenSelected(Submission? entity)
    {
        if (entity is null)
            throw new BusinessException(SubmissionsMessages.EntityNotFound);

        return Task.CompletedTask;
    }

    public Task SubmissionShouldBeAccessibleForUser(Submission entity, Guid? requestedByUserId, bool canManageAllSubmissions)
    {
        if (canManageAllSubmissions)
            return Task.CompletedTask;

        if (requestedByUserId.HasValue && requestedByUserId.Value != Guid.Empty && entity.CreatedByUserId == requestedByUserId.Value)
            return Task.CompletedTask;

        // Yetkisiz erişimde kayıt varlığını sızdırmamak için bulunamadı mesajı kullanılır.
        throw new BusinessException(SubmissionsMessages.EntityNotFound);
    }

    public Task SubmissionShouldBeEditable(Submission entity, bool canManageAllSubmissions = false)
    {
        if (canManageAllSubmissions)
            return Task.CompletedTask;

        if (IsEditableByAuthor(entity))
            return Task.CompletedTask;

        throw new BusinessException(SubmissionsMessages.SubmissionCannotBeUpdated);
    }

    public static bool IsEditableByAuthor(Submission entity)
    {
        return IsEditableByAuthor(entity.TransactionStatus, entity.IsSubmitted);
    }

    public static bool IsEditableByAuthor(TransactionStatus? transactionStatus, bool isSubmitted)
    {
        // Yazar tarafında güncelleme sadece taslak/revizyon gibi açık durumlarda yapılabilir.
        // IsSubmitted eski kayıtlarda hatalı kalsa bile ACCEPTED / REJECTED / SUBMITTED gibi
        // kapalı workflow durumları kesin olarak yazar güncellemesine kapalıdır.
        if (transactionStatus is not null)
        {
            string statusCode = transactionStatus.Code?.Trim() ?? string.Empty;
            if (AuthorClosedStatusCodes.Contains(statusCode))
                return false;

            return transactionStatus.IsActive && transactionStatus.DeletedDate == null && transactionStatus.IsEditable;
        }

        // Eski/eksik workflow verisi olan taslak kayıtlar için geriye dönük uyumluluk.
        return !isSubmitted;
    }
}
