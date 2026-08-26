namespace Symplify.BackOffice.Application.Features.Organizations.Constants;

public static class OrganizationResourceKeys
{
    public static class Validation
    {
        public const string EntityNotFound = OrganizationsMessages.EntityNotFound;
        public const string InvalidOrganizationId = OrganizationsMessages.InvalidOrganizationId;
        public const string NameRequired = OrganizationsMessages.NameRequired;
        public const string NameMaxLength = OrganizationsMessages.NameMaxLength;
        public const string CodeRequired = OrganizationsMessages.CodeRequired;
        public const string CodeMaxLength = OrganizationsMessages.CodeMaxLength;
        public const string InvalidCode = OrganizationsMessages.InvalidCode;
        public const string CodeAlreadyExists = OrganizationsMessages.CodeAlreadyExists;
        public const string SlugAlreadyExists = OrganizationsMessages.SlugAlreadyExists;
        public const string ShortNameMaxLength = OrganizationsMessages.ShortNameMaxLength;
        public const string WebsiteUrlMaxLength = OrganizationsMessages.WebsiteUrlMaxLength;
        public const string HostUrlMaxLength = OrganizationsMessages.HostUrlMaxLength;
        public const string DescriptionMaxLength = OrganizationsMessages.DescriptionMaxLength;
        public const string ContactNameMaxLength = OrganizationsMessages.ContactNameMaxLength;
        public const string ContactTitleMaxLength = OrganizationsMessages.ContactTitleMaxLength;
        public const string ContactEmailMaxLength = OrganizationsMessages.ContactEmailMaxLength;
        public const string InvalidContactEmail = OrganizationsMessages.InvalidContactEmail;
        public const string ContactPhoneMaxLength = OrganizationsMessages.ContactPhoneMaxLength;
        public const string ContactNoteMaxLength = OrganizationsMessages.ContactNoteMaxLength;
        public const string LogoLightPathMaxLength = OrganizationsMessages.LogoLightPathMaxLength;
        public const string LogoDarkPathMaxLength = OrganizationsMessages.LogoDarkPathMaxLength;
        public const string BrandColorMaxLength = OrganizationsMessages.BrandColorMaxLength;
        public const string InvalidBrandColor = OrganizationsMessages.InvalidBrandColor;
        public const string InvalidLogo = OrganizationsMessages.InvalidLogo;
        public const string InvalidWebsiteUrl = OrganizationsMessages.InvalidWebsiteUrl;
        public const string OrganizationHasCongressesCannotBeDeleted = OrganizationsMessages.OrganizationHasCongressesCannotBeDeleted;
        public const string OrganizationHasUsersCannotBeDeleted = OrganizationsMessages.OrganizationHasUsersCannotBeDeleted;
    }

    public static class Messages
    {
        public const string Created = "BackOffice.Organizations.Messages.Created";
        public const string Updated = "BackOffice.Organizations.Messages.Updated";
        public const string Deleted = "BackOffice.Organizations.Messages.Deleted";
        public const string DeleteConfirm = "BackOffice.Organizations.Messages.DeleteConfirm";
    }
}
