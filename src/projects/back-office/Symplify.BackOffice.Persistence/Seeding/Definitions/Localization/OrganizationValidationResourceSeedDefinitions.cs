namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class OrganizationValidationResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        // Organizations - validation
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.InvalidOrganizationId", "Geçersiz organizasyon bilgisi.", "Invalid organization information."),
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.SlugAlreadyExists", "Bu organizasyon kısa adresi zaten kayıtlı.", "This organization slug is already in use."),
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.WebsiteUrlMaxLength", "Web sitesi adresi en fazla 500 karakter olabilir.", "Website URL can be at most 500 characters."),
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.HostUrlMaxLength", "Host adresi en fazla 500 karakter olabilir.", "Host URL can be at most 500 characters."),
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.DescriptionMaxLength", "Açıklama en fazla 1000 karakter olabilir.", "Description can be at most 1000 characters."),
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.ContactNameMaxLength", "Yetkili kişi en fazla 200 karakter olabilir.", "Contact person can be at most 200 characters."),
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.ContactTitleMaxLength", "Yetkili görevi en fazla 200 karakter olabilir.", "Contact title can be at most 200 characters."),
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.ContactEmailMaxLength", "E-posta en fazla 256 karakter olabilir.", "Email can be at most 256 characters."),
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.ContactPhoneMaxLength", "Telefon en fazla 50 karakter olabilir.", "Phone can be at most 50 characters."),
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.ContactNoteMaxLength", "Adres / not en fazla 1000 karakter olabilir.", "Address / note can be at most 1000 characters."),
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.LogoLightPathMaxLength", "Açık tema logo yolu en fazla 500 karakter olabilir.", "Light theme logo path can be at most 500 characters."),
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.LogoDarkPathMaxLength", "Koyu tema logo yolu en fazla 500 karakter olabilir.", "Dark theme logo path can be at most 500 characters."),
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.BrandColorMaxLength", "Marka rengi en fazla 20 karakter olabilir.", "Brand color can be at most 20 characters."),
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.InvalidBrandColor", "Marka rengi #RRGGBB formatında olmalıdır.", "Brand color must be in #RRGGBB format."),
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.OrganizationHasCongressesCannotBeDeleted", "Bu organizasyona bağlı kongreler bulunduğu için organizasyon silinemez.", "This organization cannot be deleted because it has related congresses."),
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.OrganizationHasUsersCannotBeDeleted", "Bu organizasyona bağlı kullanıcılar bulunduğu için organizasyon silinemez.", "This organization cannot be deleted because it has related users."),
        new("BackOffice.Organizations.Messages", "BackOffice.Organizations.Messages.DeleteConfirm", "Bu organizasyonu silmek istediğinize emin misiniz?", "Are you sure you want to delete this organization?"),

        // Organization API Keys - validation
        new("BackOffice.OrganizationApiKeys.Validation", "BackOffice.OrganizationApiKeys.Validation.InvalidRequest", "Geçersiz API key isteği.", "Invalid API key request."),
        new("BackOffice.OrganizationApiKeys.Validation", "BackOffice.OrganizationApiKeys.Validation.OrganizationPassive", "Pasif organizasyon için aktif API key oluşturulamaz veya aktifleştirilemez.", "An active API key cannot be created or activated for a passive organization."),
        new("BackOffice.OrganizationApiKeys.Validation", "BackOffice.OrganizationApiKeys.Validation.NameAlreadyExists", "Bu organizasyon için aynı API key adı zaten kullanılıyor.", "An API key with the same name already exists for this organization."),
        new("BackOffice.OrganizationApiKeys.Validation", "BackOffice.OrganizationApiKeys.Validation.OrganizationCannotBeChanged", "API key farklı bir organizasyona taşınamaz.", "API key cannot be moved to a different organization."),
        new("BackOffice.OrganizationApiKeys.Validation", "BackOffice.OrganizationApiKeys.Validation.RevokedApiKeyCannotBeUpdated", "İptal edilmiş API key güncellenemez.", "A revoked API key cannot be updated."),
        new("BackOffice.OrganizationApiKeys.Validation", "BackOffice.OrganizationApiKeys.Validation.EnvironmentMaxLength", "Ortam bilgisi en fazla 40 karakter olabilir.", "Environment can be at most 40 characters."),
        new("BackOffice.OrganizationApiKeys.Validation", "BackOffice.OrganizationApiKeys.Validation.InvalidEnvironment", "Geçersiz API key ortamı.", "Invalid API key environment."),
        new("BackOffice.OrganizationApiKeys.Validation", "BackOffice.OrganizationApiKeys.Validation.KeyTypeMaxLength", "Anahtar tipi en fazla 40 karakter olabilir.", "Key type can be at most 40 characters."),
        new("BackOffice.OrganizationApiKeys.Validation", "BackOffice.OrganizationApiKeys.Validation.InvalidKeyType", "Geçersiz API key tipi.", "Invalid API key type."),
        new("BackOffice.OrganizationApiKeys.Validation", "BackOffice.OrganizationApiKeys.Validation.InvalidScope", "Geçersiz API key yetki kapsamı.", "Invalid API key scope."),
        new("BackOffice.OrganizationApiKeys.Validation", "BackOffice.OrganizationApiKeys.Validation.DescriptionMaxLength", "Açıklama en fazla 1000 karakter olabilir.", "Description can be at most 1000 characters."),
        new("BackOffice.OrganizationApiKeys.Validation", "BackOffice.OrganizationApiKeys.Validation.AllowedIpAddressesMaxLength", "İzinli IP adresleri alanı en fazla 2000 karakter olabilir.", "Allowed IP addresses can be at most 2000 characters."),
        new("BackOffice.OrganizationApiKeys.Validation", "BackOffice.OrganizationApiKeys.Validation.AllowedDomainsMaxLength", "İzinli domainler alanı en fazla 2000 karakter olabilir.", "Allowed domains can be at most 2000 characters."),
    };
}
