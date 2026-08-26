namespace Symplify.BackOffice.Application.Features.EventRooms.Constants;

public static class EventRoomResourceKeys
{
    public static class Page
    {
        public const string PageTitle = "BackOffice.EventRooms.PageTitle";
        public const string PageDescription = "BackOffice.EventRooms.PageDescription";
        public const string ManagementTitle = "BackOffice.EventRooms.ManagementTitle";
        public const string ManagementDescription = "BackOffice.EventRooms.ManagementDescription";
        public const string ListTitle = "BackOffice.EventRooms.ListTitle";
        public const string ListDescription = "BackOffice.EventRooms.ListDescription";
        public const string CreateButton = "BackOffice.EventRooms.CreateButton";
    }

    public static class Modal
    {
        public const string CreateTitle = "BackOffice.EventRooms.CreateModalTitle";
        public const string UpdateTitle = "BackOffice.EventRooms.UpdateModalTitle";
        public const string TranslationDescription = "BackOffice.EventRooms.TranslationModalDescription";
    }

    public static class Fields
    {
        public const string Name = "BackOffice.EventRooms.Fields.Name";
        public const string Description = "BackOffice.EventRooms.Fields.Description";
    }

    public static class Placeholders
    {
        public const string Name = "BackOffice.EventRooms.Placeholders.Name";
        public const string Description = "BackOffice.EventRooms.Placeholders.Description";
    }

    public static class Validation
    {
        public const string EntityNotFound = "BackOffice.EventRooms.Validation.EntityNotFound";
        public const string TranslationNotFound = "BackOffice.EventRooms.Validation.TranslationNotFound";
        public const string DefaultTranslationRequired = "BackOffice.EventRooms.Validation.DefaultTranslationRequired";
        public const string DefaultTranslationCannotBeDeleted = "BackOffice.EventRooms.Validation.DefaultTranslationCannotBeDeleted";
        public const string AtLeastOneTranslationRequired = "BackOffice.EventRooms.Validation.AtLeastOneTranslationRequired";
        public const string LanguageRequired = "BackOffice.EventRooms.Validation.LanguageRequired";
        public const string NameRequired = "BackOffice.EventRooms.Validation.NameRequired";
        public const string NameMaxLength = "BackOffice.EventRooms.Validation.NameMaxLength";
        public const string DescriptionMaxLength = "BackOffice.EventRooms.Validation.DescriptionMaxLength";
        public const string NameAlreadyExists = "BackOffice.EventRooms.Validation.NameAlreadyExists";
        public const string DuplicateTranslationNameInRequest = "BackOffice.EventRooms.Validation.DuplicateTranslationNameInRequest";
    }

    public static class Messages
    {
        public const string Created = "BackOffice.EventRooms.Messages.Created";
        public const string Updated = "BackOffice.EventRooms.Messages.Updated";
        public const string Deleted = "BackOffice.EventRooms.Messages.Deleted";
        public const string DeleteConfirm = "BackOffice.EventRooms.Messages.DeleteConfirm";
        public const string InvalidRecord = "BackOffice.EventRooms.Messages.InvalidRecord";
    }
}
