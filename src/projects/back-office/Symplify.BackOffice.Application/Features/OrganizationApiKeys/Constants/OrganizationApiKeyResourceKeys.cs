namespace Symplify.BackOffice.Application.Features.OrganizationApiKeys.Constants;

public static class OrganizationApiKeyResourceKeys
{
    public static class Validation
    {
        public const string EntityNotFound = OrganizationApiKeysMessages.EntityNotFound;
        public const string InvalidRequest = OrganizationApiKeysMessages.InvalidRequest;
        public const string OrganizationNotFound = OrganizationApiKeysMessages.OrganizationNotFound;
        public const string OrganizationRequired = OrganizationApiKeysMessages.OrganizationRequired;
        public const string OrganizationPassive = OrganizationApiKeysMessages.OrganizationPassive;
        public const string NameRequired = OrganizationApiKeysMessages.NameRequired;
        public const string NameMaxLength = OrganizationApiKeysMessages.NameMaxLength;
        public const string NameAlreadyExists = OrganizationApiKeysMessages.NameAlreadyExists;
        public const string OrganizationCannotBeChanged = OrganizationApiKeysMessages.OrganizationCannotBeChanged;
        public const string RevokedApiKeyCannotBeUpdated = OrganizationApiKeysMessages.RevokedApiKeyCannotBeUpdated;
        public const string EnvironmentRequired = OrganizationApiKeysMessages.EnvironmentRequired;
        public const string EnvironmentMaxLength = OrganizationApiKeysMessages.EnvironmentMaxLength;
        public const string InvalidEnvironment = OrganizationApiKeysMessages.InvalidEnvironment;
        public const string KeyTypeRequired = OrganizationApiKeysMessages.KeyTypeRequired;
        public const string KeyTypeMaxLength = OrganizationApiKeysMessages.KeyTypeMaxLength;
        public const string InvalidKeyType = OrganizationApiKeysMessages.InvalidKeyType;
        public const string InvalidScope = OrganizationApiKeysMessages.InvalidScope;
        public const string AtLeastOneScopeRequired = OrganizationApiKeysMessages.AtLeastOneScopeRequired;
        public const string ExpiresAtMustBeFuture = OrganizationApiKeysMessages.ExpiresAtMustBeFuture;
        public const string DescriptionMaxLength = OrganizationApiKeysMessages.DescriptionMaxLength;
        public const string AllowedIpAddressesMaxLength = OrganizationApiKeysMessages.AllowedIpAddressesMaxLength;
        public const string AllowedDomainsMaxLength = OrganizationApiKeysMessages.AllowedDomainsMaxLength;
    }

    public static class Messages
    {
        public const string Created = "BackOffice.OrganizationApiKeys.Messages.Created";
        public const string Updated = "BackOffice.OrganizationApiKeys.Messages.Updated";
        public const string Deleted = "BackOffice.OrganizationApiKeys.Messages.Deleted";
        public const string Revoked = "BackOffice.OrganizationApiKeys.Messages.Revoked";
        public const string InvalidApiKey = "BackOffice.OrganizationApiKeys.Messages.InvalidApiKey";
    }
}
