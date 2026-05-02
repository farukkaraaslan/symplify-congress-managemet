namespace Symplify.BackOffice.Application.Features.DocumentTypes.Constants;

public static class DocumentTypeResourceKeys
{
    public static class Page
    {
        public const string PageTitle = "BackOffice.DocumentTypes.PageTitle";
        public const string PageDescription = "BackOffice.DocumentTypes.PageDescription";
        public const string ManagementTitle = "BackOffice.DocumentTypes.ManagementTitle";
        public const string ManagementDescription = "BackOffice.DocumentTypes.ManagementDescription";
        public const string ListTitle = "BackOffice.DocumentTypes.ListTitle";
        public const string ListDescription = "BackOffice.DocumentTypes.ListDescription";
        public const string CreateButton = "BackOffice.DocumentTypes.CreateButton";
    }

    public static class Modal
    {
        public const string CreateTitle = "BackOffice.DocumentTypes.CreateModalTitle";
        public const string UpdateTitle = "BackOffice.DocumentTypes.UpdateModalTitle";
        public const string TranslationDescription = "BackOffice.DocumentTypes.TranslationModalDescription";
    }

    public static class Fields
    {
        public const string Name = "BackOffice.DocumentTypes.Fields.Name";
        public const string Description = "BackOffice.DocumentTypes.Fields.Description";
    }

    public static class Placeholders
    {
        public const string Name = "BackOffice.DocumentTypes.Placeholders.Name";
        public const string Description = "BackOffice.DocumentTypes.Placeholders.Description";
    }

    public static class Validation
    {
        public const string EntityNotFound = "BackOffice.DocumentTypes.Validation.EntityNotFound";
        public const string TranslationNotFound = "BackOffice.DocumentTypes.Validation.TranslationNotFound";
        public const string DefaultTranslationRequired = "BackOffice.DocumentTypes.Validation.DefaultTranslationRequired";
        public const string DefaultTranslationCannotBeDeleted = "BackOffice.DocumentTypes.Validation.DefaultTranslationCannotBeDeleted";
        public const string AtLeastOneTranslationRequired = "BackOffice.DocumentTypes.Validation.AtLeastOneTranslationRequired";
        public const string LanguageRequired = "BackOffice.DocumentTypes.Validation.LanguageRequired";
        public const string NameRequired = "BackOffice.DocumentTypes.Validation.NameRequired";
        public const string NameMaxLength = "BackOffice.DocumentTypes.Validation.NameMaxLength";
        public const string DescriptionMaxLength = "BackOffice.DocumentTypes.Validation.DescriptionMaxLength";
        public const string NameAlreadyExists = "BackOffice.DocumentTypes.Validation.NameAlreadyExists";
        public const string DuplicateTranslationNameInRequest = "BackOffice.DocumentTypes.Validation.DuplicateTranslationNameInRequest";
    }

    public static class Messages
    {
        public const string Created = "BackOffice.DocumentTypes.Messages.Created";
        public const string Updated = "BackOffice.DocumentTypes.Messages.Updated";
        public const string Deleted = "BackOffice.DocumentTypes.Messages.Deleted";
        public const string DeleteConfirm = "BackOffice.DocumentTypes.Messages.DeleteConfirm";
        public const string InvalidRecord = "BackOffice.DocumentTypes.Messages.InvalidRecord";
    }
}
