namespace Symplify.BackOffice.Application.Features.Topics.Constants;

public static class TopicResourceKeys
{
    public static class Page
    {
        public const string PageTitle = "BackOffice.Topics.PageTitle";
        public const string PageDescription = "BackOffice.Topics.PageDescription";
        public const string ManagementTitle = "BackOffice.Topics.ManagementTitle";
        public const string ManagementDescription = "BackOffice.Topics.ManagementDescription";
        public const string ListTitle = "BackOffice.Topics.ListTitle";
        public const string ListDescription = "BackOffice.Topics.ListDescription";
        public const string CreateButton = "BackOffice.Topics.CreateButton";
    }

    public static class Modal
    {
        public const string CreateTitle = "BackOffice.Topics.CreateModalTitle";
        public const string UpdateTitle = "BackOffice.Topics.UpdateModalTitle";
        public const string TranslationDescription = "BackOffice.Topics.TranslationModalDescription";
    }

    public static class Fields
    {
        public const string Name = "BackOffice.Topics.Fields.Name";
        public const string Description = "BackOffice.Topics.Fields.Description";
    }

    public static class Placeholders
    {
        public const string Name = "BackOffice.Topics.Placeholders.Name";
        public const string Description = "BackOffice.Topics.Placeholders.Description";
    }

    public static class Validation
    {
        public const string EntityNotFound = "BackOffice.Topics.Validation.EntityNotFound";
        public const string TranslationNotFound = "BackOffice.Topics.Validation.TranslationNotFound";
        public const string DefaultTranslationRequired = "BackOffice.Topics.Validation.DefaultTranslationRequired";
        public const string DefaultTranslationCannotBeDeleted = "BackOffice.Topics.Validation.DefaultTranslationCannotBeDeleted";
        public const string AtLeastOneTranslationRequired = "BackOffice.Topics.Validation.AtLeastOneTranslationRequired";
        public const string LanguageRequired = "BackOffice.Topics.Validation.LanguageRequired";
        public const string NameRequired = "BackOffice.Topics.Validation.NameRequired";
        public const string NameMaxLength = "BackOffice.Topics.Validation.NameMaxLength";
        public const string DescriptionMaxLength = "BackOffice.Topics.Validation.DescriptionMaxLength";
        public const string NameAlreadyExists = "BackOffice.Topics.Validation.NameAlreadyExists";
        public const string DuplicateTranslationNameInRequest = "BackOffice.Topics.Validation.DuplicateTranslationNameInRequest";
    }

    public static class Messages
    {
        public const string Created = "BackOffice.Topics.Messages.Created";
        public const string Updated = "BackOffice.Topics.Messages.Updated";
        public const string Deleted = "BackOffice.Topics.Messages.Deleted";
        public const string NotFound = "BackOffice.Topics.Messages.NotFound";
        public const string InvalidTopic = "BackOffice.Topics.Messages.InvalidTopic";
    }
}