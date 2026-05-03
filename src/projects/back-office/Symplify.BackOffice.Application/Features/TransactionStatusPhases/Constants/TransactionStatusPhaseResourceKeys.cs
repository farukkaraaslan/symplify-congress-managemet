namespace Symplify.BackOffice.Application.Features.TransactionStatusPhases.Constants;

public static class TransactionStatusPhaseResourceKeys
{
    public static class Page
    {
        public const string PageTitle = "BackOffice.TransactionStatusPhases.PageTitle";
        public const string PageDescription = "BackOffice.TransactionStatusPhases.PageDescription";
        public const string ManagementTitle = "BackOffice.TransactionStatusPhases.ManagementTitle";
        public const string ManagementDescription = "BackOffice.TransactionStatusPhases.ManagementDescription";
        public const string ListTitle = "BackOffice.TransactionStatusPhases.ListTitle";
        public const string ListDescription = "BackOffice.TransactionStatusPhases.ListDescription";
        public const string CreateButton = "BackOffice.TransactionStatusPhases.CreateButton";
    }

    public static class Modal
    {
        public const string CreateTitle = "BackOffice.TransactionStatusPhases.CreateModalTitle";
        public const string UpdateTitle = "BackOffice.TransactionStatusPhases.UpdateModalTitle";
        public const string TranslationDescription = "BackOffice.TransactionStatusPhases.TranslationModalDescription";
    }

    public static class Fields
    {
        public const string Code = "BackOffice.TransactionStatusPhases.Fields.Code";
        public const string Name = "BackOffice.TransactionStatusPhases.Fields.Name";
        public const string Description = "BackOffice.TransactionStatusPhases.Fields.Description";
        public const string Order = "BackOffice.TransactionStatusPhases.Fields.Order";
        public const string IsActive = "BackOffice.TransactionStatusPhases.Fields.IsActive";
    }

    public static class Placeholders
    {
        public const string Code = "BackOffice.TransactionStatusPhases.Placeholders.Code";
        public const string Name = "BackOffice.TransactionStatusPhases.Placeholders.Name";
        public const string Description = "BackOffice.TransactionStatusPhases.Placeholders.Description";
    }

    public static class Validation
    {
        public const string EntityNotFound = "BackOffice.TransactionStatusPhases.Validation.EntityNotFound";
        public const string TranslationNotFound = "BackOffice.TransactionStatusPhases.Validation.TranslationNotFound";
        public const string DefaultTranslationRequired = "BackOffice.TransactionStatusPhases.Validation.DefaultTranslationRequired";
        public const string DefaultTranslationCannotBeDeleted = "BackOffice.TransactionStatusPhases.Validation.DefaultTranslationCannotBeDeleted";
        public const string AtLeastOneTranslationRequired = "BackOffice.TransactionStatusPhases.Validation.AtLeastOneTranslationRequired";
        public const string LanguageRequired = "BackOffice.TransactionStatusPhases.Validation.LanguageRequired";
        public const string CodeRequired = "BackOffice.TransactionStatusPhases.Validation.CodeRequired";
        public const string CodeMaxLength = "BackOffice.TransactionStatusPhases.Validation.CodeMaxLength";
        public const string CodeAlreadyExists = "BackOffice.TransactionStatusPhases.Validation.CodeAlreadyExists";
        public const string OrderCannotBeNegative = "BackOffice.TransactionStatusPhases.Validation.OrderCannotBeNegative";
        public const string NameRequired = "BackOffice.TransactionStatusPhases.Validation.NameRequired";
        public const string NameMaxLength = "BackOffice.TransactionStatusPhases.Validation.NameMaxLength";
        public const string DescriptionMaxLength = "BackOffice.TransactionStatusPhases.Validation.DescriptionMaxLength";
        public const string DuplicateTranslationNameInRequest = "BackOffice.TransactionStatusPhases.Validation.DuplicateTranslationNameInRequest";
        public const string NameAlreadyExists = "BackOffice.TransactionStatusPhases.Validation.NameAlreadyExists";
        public const string EntityUsedByTransactionStatus = "BackOffice.TransactionStatusPhases.Validation.EntityUsedByTransactionStatus";
        public const string ReorderEntityNotFound = "BackOffice.TransactionStatusPhases.Validation.ReorderEntityNotFound";
    }

    public static class Messages
    {
        public const string Created = "BackOffice.TransactionStatusPhases.Messages.Created";
        public const string Updated = "BackOffice.TransactionStatusPhases.Messages.Updated";
        public const string Deleted = "BackOffice.TransactionStatusPhases.Messages.Deleted";
        public const string DeleteConfirm = "BackOffice.TransactionStatusPhases.Messages.DeleteConfirm";
        public const string InvalidRecord = "BackOffice.TransactionStatusPhases.Messages.InvalidRecord";
        public const string Reordered = "BackOffice.TransactionStatusPhases.Messages.Reordered";
    }
}
