namespace Symplify.BackOffice.Application.Features.TransactionStatuses.Constants;

public static class TransactionStatusResourceKeys
{
    public static class Page
    {
        public const string PageTitle = "BackOffice.TransactionStatuses.PageTitle";
        public const string PageDescription = "BackOffice.TransactionStatuses.PageDescription";
        public const string ManagementTitle = "BackOffice.TransactionStatuses.ManagementTitle";
        public const string ManagementDescription = "BackOffice.TransactionStatuses.ManagementDescription";
        public const string ListTitle = "BackOffice.TransactionStatuses.ListTitle";
        public const string ListDescription = "BackOffice.TransactionStatuses.ListDescription";
        public const string CreateButton = "BackOffice.TransactionStatuses.CreateButton";
    }

    public static class Modal
    {
        public const string CreateTitle = "BackOffice.TransactionStatuses.CreateModalTitle";
        public const string UpdateTitle = "BackOffice.TransactionStatuses.UpdateModalTitle";
        public const string TranslationDescription = "BackOffice.TransactionStatuses.TranslationModalDescription";
    }

    public static class Fields
    {
        public const string Phase = "BackOffice.TransactionStatuses.Fields.Phase";
        public const string Code = "BackOffice.TransactionStatuses.Fields.Code";
        public const string Name = "BackOffice.TransactionStatuses.Fields.Name";
        public const string Description = "BackOffice.TransactionStatuses.Fields.Description";
        public const string Order = "BackOffice.TransactionStatuses.Fields.Order";
        public const string IsEditable = "BackOffice.TransactionStatuses.Fields.IsEditable";
        public const string IsFinal = "BackOffice.TransactionStatuses.Fields.IsFinal";
        public const string IsActive = "BackOffice.TransactionStatuses.Fields.IsActive";
    }

    public static class Placeholders
    {
        public const string Phase = "BackOffice.TransactionStatuses.Placeholders.Phase";
        public const string Code = "BackOffice.TransactionStatuses.Placeholders.Code";
        public const string Name = "BackOffice.TransactionStatuses.Placeholders.Name";
        public const string Description = "BackOffice.TransactionStatuses.Placeholders.Description";
    }

    public static class Validation
    {
        public const string EntityNotFound = "BackOffice.TransactionStatuses.Validation.EntityNotFound";
        public const string TranslationNotFound = "BackOffice.TransactionStatuses.Validation.TranslationNotFound";
        public const string DefaultTranslationRequired = "BackOffice.TransactionStatuses.Validation.DefaultTranslationRequired";
        public const string DefaultTranslationCannotBeDeleted = "BackOffice.TransactionStatuses.Validation.DefaultTranslationCannotBeDeleted";
        public const string AtLeastOneTranslationRequired = "BackOffice.TransactionStatuses.Validation.AtLeastOneTranslationRequired";
        public const string LanguageRequired = "BackOffice.TransactionStatuses.Validation.LanguageRequired";
        public const string PhaseRequired = "BackOffice.TransactionStatuses.Validation.PhaseRequired";
        public const string PhaseNotFound = "BackOffice.TransactionStatuses.Validation.PhaseNotFound";
        public const string PhasePassive = "BackOffice.TransactionStatuses.Validation.PhasePassive";
        public const string CodeRequired = "BackOffice.TransactionStatuses.Validation.CodeRequired";
        public const string CodeMaxLength = "BackOffice.TransactionStatuses.Validation.CodeMaxLength";
        public const string CodeAlreadyExists = "BackOffice.TransactionStatuses.Validation.CodeAlreadyExists";
        public const string OrderCannotBeNegative = "BackOffice.TransactionStatuses.Validation.OrderCannotBeNegative";
        public const string NameRequired = "BackOffice.TransactionStatuses.Validation.NameRequired";
        public const string NameMaxLength = "BackOffice.TransactionStatuses.Validation.NameMaxLength";
        public const string DescriptionMaxLength = "BackOffice.TransactionStatuses.Validation.DescriptionMaxLength";
        public const string DuplicateTranslationNameInRequest = "BackOffice.TransactionStatuses.Validation.DuplicateTranslationNameInRequest";
        public const string EntityUsedByTransition = "BackOffice.TransactionStatuses.Validation.EntityUsedByTransition";
        public const string EntityUsedBySubmission = "BackOffice.TransactionStatuses.Validation.EntityUsedBySubmission";
        public const string EntityUsedBySubmissionHistory = "BackOffice.TransactionStatuses.Validation.EntityUsedBySubmissionHistory";
        public const string FinalStatusCannotHaveOutgoingTransition = "BackOffice.TransactionStatuses.Validation.FinalStatusCannotHaveOutgoingTransition";
        public const string ReorderEntityNotFound = "BackOffice.TransactionStatuses.Validation.ReorderEntityNotFound";
        public const string ReorderDifferentPhaseNotAllowed = "BackOffice.TransactionStatuses.Validation.ReorderDifferentPhaseNotAllowed";
    }

    public static class Messages
    {
        public const string Created = "BackOffice.TransactionStatuses.Messages.Created";
        public const string Updated = "BackOffice.TransactionStatuses.Messages.Updated";
        public const string Deleted = "BackOffice.TransactionStatuses.Messages.Deleted";
        public const string DeleteConfirm = "BackOffice.TransactionStatuses.Messages.DeleteConfirm";
        public const string InvalidRecord = "BackOffice.TransactionStatuses.Messages.InvalidRecord";
        public const string Reordered = "BackOffice.TransactionStatuses.Messages.Reordered";
    }
}
