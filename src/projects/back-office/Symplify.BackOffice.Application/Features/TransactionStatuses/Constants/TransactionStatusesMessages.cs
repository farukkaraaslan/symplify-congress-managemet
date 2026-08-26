namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Constants;

public static class TransactionStatusesMessages
{
    public const string EntityNotFound = TransactionStatusResourceKeys.Validation.EntityNotFound;
    public const string TranslationNotFound = TransactionStatusResourceKeys.Validation.TranslationNotFound;
    public const string DefaultTranslationRequired = TransactionStatusResourceKeys.Validation.DefaultTranslationRequired;
    public const string DefaultTranslationCannotBeDeleted = TransactionStatusResourceKeys.Validation.DefaultTranslationCannotBeDeleted;
    public const string AtLeastOneTranslationRequired = TransactionStatusResourceKeys.Validation.AtLeastOneTranslationRequired;
    public const string LanguageRequired = TransactionStatusResourceKeys.Validation.LanguageRequired;
    public const string PhaseRequired = TransactionStatusResourceKeys.Validation.PhaseRequired;
    public const string PhaseNotFound = TransactionStatusResourceKeys.Validation.PhaseNotFound;
    public const string PhasePassive = TransactionStatusResourceKeys.Validation.PhasePassive;
    public const string CodeRequired = TransactionStatusResourceKeys.Validation.CodeRequired;
    public const string CodeMaxLength = TransactionStatusResourceKeys.Validation.CodeMaxLength;
    public const string CodeAlreadyExists = TransactionStatusResourceKeys.Validation.CodeAlreadyExists;
    public const string OrderCannotBeNegative = TransactionStatusResourceKeys.Validation.OrderCannotBeNegative;
    public const string NameRequired = TransactionStatusResourceKeys.Validation.NameRequired;
    public const string NameMaxLength = TransactionStatusResourceKeys.Validation.NameMaxLength;
    public const string DescriptionMaxLength = TransactionStatusResourceKeys.Validation.DescriptionMaxLength;
    public const string DuplicateTranslationNameInRequest = TransactionStatusResourceKeys.Validation.DuplicateTranslationNameInRequest;
    public const string EntityUsedByTransition = TransactionStatusResourceKeys.Validation.EntityUsedByTransition;
    public const string EntityUsedBySubmission = TransactionStatusResourceKeys.Validation.EntityUsedBySubmission;
    public const string EntityUsedBySubmissionHistory = TransactionStatusResourceKeys.Validation.EntityUsedBySubmissionHistory;
    public const string FinalStatusCannotHaveOutgoingTransition = TransactionStatusResourceKeys.Validation.FinalStatusCannotHaveOutgoingTransition;
    public const string ReorderEntityNotFound = TransactionStatusResourceKeys.Validation.ReorderEntityNotFound;
    public const string ReorderDifferentPhaseNotAllowed = TransactionStatusResourceKeys.Validation.ReorderDifferentPhaseNotAllowed;

    public const string Created = TransactionStatusResourceKeys.Messages.Created;
    public const string Updated = TransactionStatusResourceKeys.Messages.Updated;
    public const string Deleted = TransactionStatusResourceKeys.Messages.Deleted;
    public const string DeleteConfirm = TransactionStatusResourceKeys.Messages.DeleteConfirm;
    public const string InvalidRecord = TransactionStatusResourceKeys.Messages.InvalidRecord;
    public const string Reordered = TransactionStatusResourceKeys.Messages.Reordered;
}
