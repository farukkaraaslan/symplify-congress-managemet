namespace Symplify.BackOffice.Application.Features.SubmissionTypes.Constants;

public static class SubmissionTypeResourceKeys
{
    public static class Page
    {
        public const string PageTitle = "BackOffice.SubmissionTypes.PageTitle";
        public const string PageDescription = "BackOffice.SubmissionTypes.PageDescription";
        public const string ManagementTitle = "BackOffice.SubmissionTypes.ManagementTitle";
        public const string ManagementDescription = "BackOffice.SubmissionTypes.ManagementDescription";
        public const string ListTitle = "BackOffice.SubmissionTypes.ListTitle";
        public const string ListDescription = "BackOffice.SubmissionTypes.ListDescription";
        public const string CreateButton = "BackOffice.SubmissionTypes.CreateButton";
    }

    public static class Modal
    {
        public const string CreateTitle = "BackOffice.SubmissionTypes.CreateModalTitle";
        public const string UpdateTitle = "BackOffice.SubmissionTypes.UpdateModalTitle";
        public const string TranslationDescription = "BackOffice.SubmissionTypes.TranslationModalDescription";
    }

    public static class Fields
    {
        public const string Name = "BackOffice.SubmissionTypes.Fields.Name";
        public const string Description = "BackOffice.SubmissionTypes.Fields.Description";
    }

    public static class Placeholders
    {
        public const string Name = "BackOffice.SubmissionTypes.Placeholders.Name";
        public const string Description = "BackOffice.SubmissionTypes.Placeholders.Description";
    }

    public static class Validation
    {
        public const string EntityNotFound = "BackOffice.SubmissionTypes.Validation.EntityNotFound";
        public const string TranslationNotFound = "BackOffice.SubmissionTypes.Validation.TranslationNotFound";
        public const string DefaultTranslationRequired = "BackOffice.SubmissionTypes.Validation.DefaultTranslationRequired";
        public const string DefaultTranslationCannotBeDeleted = "BackOffice.SubmissionTypes.Validation.DefaultTranslationCannotBeDeleted";
        public const string AtLeastOneTranslationRequired = "BackOffice.SubmissionTypes.Validation.AtLeastOneTranslationRequired";
        public const string LanguageRequired = "BackOffice.SubmissionTypes.Validation.LanguageRequired";
        public const string NameRequired = "BackOffice.SubmissionTypes.Validation.NameRequired";
        public const string NameMaxLength = "BackOffice.SubmissionTypes.Validation.NameMaxLength";
        public const string DescriptionMaxLength = "BackOffice.SubmissionTypes.Validation.DescriptionMaxLength";
        public const string NameAlreadyExists = "BackOffice.SubmissionTypes.Validation.NameAlreadyExists";
        public const string DuplicateTranslationNameInRequest = "BackOffice.SubmissionTypes.Validation.DuplicateTranslationNameInRequest";
    }

    public static class Messages
    {
        public const string Created = "BackOffice.SubmissionTypes.Messages.Created";
        public const string Updated = "BackOffice.SubmissionTypes.Messages.Updated";
        public const string Deleted = "BackOffice.SubmissionTypes.Messages.Deleted";
        public const string DeleteConfirm = "BackOffice.SubmissionTypes.Messages.DeleteConfirm";
        public const string InvalidRecord = "BackOffice.SubmissionTypes.Messages.InvalidRecord";
    }
}
