namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Constants;

public static class TransactionStatusPhasesMessages
{
    public const string EntityNotFound = TransactionStatusPhaseResourceKeys.Validation.EntityNotFound;
    public const string TranslationNotFound = TransactionStatusPhaseResourceKeys.Validation.TranslationNotFound;
    public const string DefaultTranslationRequired = TransactionStatusPhaseResourceKeys.Validation.DefaultTranslationRequired;
    public const string DefaultTranslationCannotBeDeleted = TransactionStatusPhaseResourceKeys.Validation.DefaultTranslationCannotBeDeleted;
    public const string AtLeastOneTranslationRequired = TransactionStatusPhaseResourceKeys.Validation.AtLeastOneTranslationRequired;
    public const string LanguageRequired = TransactionStatusPhaseResourceKeys.Validation.LanguageRequired;
    public const string CodeRequired = TransactionStatusPhaseResourceKeys.Validation.CodeRequired;
    public const string CodeMaxLength = TransactionStatusPhaseResourceKeys.Validation.CodeMaxLength;
    public const string CodeAlreadyExists = TransactionStatusPhaseResourceKeys.Validation.CodeAlreadyExists;
    public const string OrderCannotBeNegative = TransactionStatusPhaseResourceKeys.Validation.OrderCannotBeNegative;
    public const string NameRequired = TransactionStatusPhaseResourceKeys.Validation.NameRequired;
    public const string NameMaxLength = TransactionStatusPhaseResourceKeys.Validation.NameMaxLength;
    public const string DescriptionMaxLength = TransactionStatusPhaseResourceKeys.Validation.DescriptionMaxLength;
    public const string DuplicateTranslationNameInRequest = TransactionStatusPhaseResourceKeys.Validation.DuplicateTranslationNameInRequest;
    public const string NameAlreadyExists = TransactionStatusPhaseResourceKeys.Validation.NameAlreadyExists;
    public const string EntityUsedByTransactionStatus = TransactionStatusPhaseResourceKeys.Validation.EntityUsedByTransactionStatus;
    public const string ReorderEntityNotFound = TransactionStatusPhaseResourceKeys.Validation.ReorderEntityNotFound;

    public const string Created = TransactionStatusPhaseResourceKeys.Messages.Created;
    public const string Updated = TransactionStatusPhaseResourceKeys.Messages.Updated;
    public const string Deleted = TransactionStatusPhaseResourceKeys.Messages.Deleted;
    public const string DeleteConfirm = TransactionStatusPhaseResourceKeys.Messages.DeleteConfirm;
    public const string InvalidRecord = TransactionStatusPhaseResourceKeys.Messages.InvalidRecord;
    public const string Reordered = TransactionStatusPhaseResourceKeys.Messages.Reordered;
}
