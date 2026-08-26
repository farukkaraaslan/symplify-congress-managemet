namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Constants;

public static class TransactionStatusTransitionsMessages
{
    public const string EntityNotFound = TransactionStatusTransitionResourceKeys.Validation.EntityNotFound;
    public const string TranslationNotFound = TransactionStatusTransitionResourceKeys.Validation.TranslationNotFound;
    public const string DefaultTranslationRequired = TransactionStatusTransitionResourceKeys.Validation.DefaultTranslationRequired;
    public const string DefaultTranslationCannotBeDeleted = TransactionStatusTransitionResourceKeys.Validation.DefaultTranslationCannotBeDeleted;
    public const string AtLeastOneTranslationRequired = TransactionStatusTransitionResourceKeys.Validation.AtLeastOneTranslationRequired;
    public const string LanguageRequired = TransactionStatusTransitionResourceKeys.Validation.LanguageRequired;
    public const string FromStatusRequired = TransactionStatusTransitionResourceKeys.Validation.FromStatusRequired;
    public const string ToStatusRequired = TransactionStatusTransitionResourceKeys.Validation.ToStatusRequired;
    public const string FromStatusNotFound = TransactionStatusTransitionResourceKeys.Validation.FromStatusNotFound;
    public const string ToStatusNotFound = TransactionStatusTransitionResourceKeys.Validation.ToStatusNotFound;
    public const string FromStatusPassive = TransactionStatusTransitionResourceKeys.Validation.FromStatusPassive;
    public const string ToStatusPassive = TransactionStatusTransitionResourceKeys.Validation.ToStatusPassive;
    public const string FromStatusCannotBeFinal = TransactionStatusTransitionResourceKeys.Validation.FromStatusCannotBeFinal;
    public const string FromAndToStatusCannotBeSame = TransactionStatusTransitionResourceKeys.Validation.FromAndToStatusCannotBeSame;
    public const string TransitionAlreadyExists = TransactionStatusTransitionResourceKeys.Validation.TransitionAlreadyExists;
    public const string NameRequired = TransactionStatusTransitionResourceKeys.Validation.NameRequired;
    public const string NameMaxLength = TransactionStatusTransitionResourceKeys.Validation.NameMaxLength;
    public const string DescriptionMaxLength = TransactionStatusTransitionResourceKeys.Validation.DescriptionMaxLength;
    public const string DuplicateTranslationNameInRequest = TransactionStatusTransitionResourceKeys.Validation.DuplicateTranslationNameInRequest;

    public const string Created = TransactionStatusTransitionResourceKeys.Messages.Created;
    public const string Updated = TransactionStatusTransitionResourceKeys.Messages.Updated;
    public const string Deleted = TransactionStatusTransitionResourceKeys.Messages.Deleted;
    public const string DeleteConfirm = TransactionStatusTransitionResourceKeys.Messages.DeleteConfirm;
    public const string InvalidRecord = TransactionStatusTransitionResourceKeys.Messages.InvalidRecord;
}
