namespace Symplify.BackOffice.Application.Features.TransactionStatusTransitions.Constants;

public static class TransactionStatusTransitionResourceKeys
{
    public static class Page
    {
        public const string PageTitle = "BackOffice.TransactionStatusTransitions.PageTitle";
        public const string PageDescription = "BackOffice.TransactionStatusTransitions.PageDescription";
        public const string ManagementTitle = "BackOffice.TransactionStatusTransitions.ManagementTitle";
        public const string ManagementDescription = "BackOffice.TransactionStatusTransitions.ManagementDescription";
        public const string ListTitle = "BackOffice.TransactionStatusTransitions.ListTitle";
        public const string ListDescription = "BackOffice.TransactionStatusTransitions.ListDescription";
        public const string CreateButton = "BackOffice.TransactionStatusTransitions.CreateButton";
    }

    public static class Modal
    {
        public const string CreateTitle = "BackOffice.TransactionStatusTransitions.CreateModalTitle";
        public const string UpdateTitle = "BackOffice.TransactionStatusTransitions.UpdateModalTitle";
        public const string TranslationDescription = "BackOffice.TransactionStatusTransitions.TranslationModalDescription";
    }

    public static class Fields
    {
        public const string FromStatus = "BackOffice.TransactionStatusTransitions.Fields.FromStatus";
        public const string ToStatus = "BackOffice.TransactionStatusTransitions.Fields.ToStatus";
        public const string Name = "BackOffice.TransactionStatusTransitions.Fields.Name";
        public const string Description = "BackOffice.TransactionStatusTransitions.Fields.Description";
        public const string IsActive = "BackOffice.TransactionStatusTransitions.Fields.IsActive";
    }

    public static class Placeholders
    {
        public const string FromStatus = "BackOffice.TransactionStatusTransitions.Placeholders.FromStatus";
        public const string ToStatus = "BackOffice.TransactionStatusTransitions.Placeholders.ToStatus";
        public const string Name = "BackOffice.TransactionStatusTransitions.Placeholders.Name";
        public const string Description = "BackOffice.TransactionStatusTransitions.Placeholders.Description";
    }

    public static class Validation
    {
        public const string EntityNotFound = "BackOffice.TransactionStatusTransitions.Validation.EntityNotFound";
        public const string TranslationNotFound = "BackOffice.TransactionStatusTransitions.Validation.TranslationNotFound";
        public const string DefaultTranslationRequired = "BackOffice.TransactionStatusTransitions.Validation.DefaultTranslationRequired";
        public const string DefaultTranslationCannotBeDeleted = "BackOffice.TransactionStatusTransitions.Validation.DefaultTranslationCannotBeDeleted";
        public const string AtLeastOneTranslationRequired = "BackOffice.TransactionStatusTransitions.Validation.AtLeastOneTranslationRequired";
        public const string LanguageRequired = "BackOffice.TransactionStatusTransitions.Validation.LanguageRequired";
        public const string FromStatusRequired = "BackOffice.TransactionStatusTransitions.Validation.FromStatusRequired";
        public const string ToStatusRequired = "BackOffice.TransactionStatusTransitions.Validation.ToStatusRequired";
        public const string FromStatusNotFound = "BackOffice.TransactionStatusTransitions.Validation.FromStatusNotFound";
        public const string ToStatusNotFound = "BackOffice.TransactionStatusTransitions.Validation.ToStatusNotFound";
        public const string FromStatusPassive = "BackOffice.TransactionStatusTransitions.Validation.FromStatusPassive";
        public const string ToStatusPassive = "BackOffice.TransactionStatusTransitions.Validation.ToStatusPassive";
        public const string FromStatusCannotBeFinal = "BackOffice.TransactionStatusTransitions.Validation.FromStatusCannotBeFinal";
        public const string FromAndToStatusCannotBeSame = "BackOffice.TransactionStatusTransitions.Validation.FromAndToStatusCannotBeSame";
        public const string TransitionAlreadyExists = "BackOffice.TransactionStatusTransitions.Validation.TransitionAlreadyExists";
        public const string NameRequired = "BackOffice.TransactionStatusTransitions.Validation.NameRequired";
        public const string NameMaxLength = "BackOffice.TransactionStatusTransitions.Validation.NameMaxLength";
        public const string DescriptionMaxLength = "BackOffice.TransactionStatusTransitions.Validation.DescriptionMaxLength";
        public const string DuplicateTranslationNameInRequest = "BackOffice.TransactionStatusTransitions.Validation.DuplicateTranslationNameInRequest";
    }

    public static class Messages
    {
        public const string Created = "BackOffice.TransactionStatusTransitions.Messages.Created";
        public const string Updated = "BackOffice.TransactionStatusTransitions.Messages.Updated";
        public const string Deleted = "BackOffice.TransactionStatusTransitions.Messages.Deleted";
        public const string DeleteConfirm = "BackOffice.TransactionStatusTransitions.Messages.DeleteConfirm";
        public const string InvalidRecord = "BackOffice.TransactionStatusTransitions.Messages.InvalidRecord";
    }
}
