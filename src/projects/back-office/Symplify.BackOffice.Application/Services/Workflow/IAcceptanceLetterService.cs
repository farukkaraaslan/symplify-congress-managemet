using Symplify.BackOffice.Domain.Submission;

namespace Symplify.BackOffice.Application.Services.Workflow;

public interface IAcceptanceLetterService
{
    Task<IReadOnlyList<SubmissionAcceptanceLetter>> GenerateAsync(
        Submission submission,
        CancellationToken cancellationToken);

    /// <summary>
    /// Recreates the current acceptance letters after accepted submission data changes.
    /// This method does not queue email. It replaces the active file records and keeps only the latest document visible.
    /// </summary>
    Task<IReadOnlyList<SubmissionAcceptanceLetter>> ReplaceCurrentAsync(
        Submission submission,
        Guid? performedByUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns true when an accepted submission has at least one active author without a current acceptance letter.
    /// This is needed for legacy production records where an author was added or changed after acceptance.
    /// </summary>
    Task<bool> HasMissingCurrentLettersAsync(
        Submission submission,
        CancellationToken cancellationToken);
}
