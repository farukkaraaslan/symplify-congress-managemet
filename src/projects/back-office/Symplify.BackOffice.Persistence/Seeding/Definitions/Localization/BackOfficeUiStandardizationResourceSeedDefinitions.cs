namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class BackOfficeUiStandardizationResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        R("Common.Back", "Geri Dön", "Back"),
        R("Common.Continue", "Devam Et", "Continue"),
        R("Common.Copy", "Kopyala", "Copy"),
        R("Common.Copied", "Kopyalandı.", "Copied."),
        R("Common.CopyFailed", "Kopyalama işlemi başarısız oldu.", "Copy operation failed."),
        R("Common.InvalidField", "Alan değeri geçerli değil.", "Field value is invalid."),

        R("BackOffice.OrganizationApiKeys.Create.Title", "Yeni API Key", "New API Key"),
        R("BackOffice.OrganizationApiKeys.Create.PageDescription", "Organizasyon API key erişimi için yeni bir anahtar oluşturun.", "Create a new key for organization API access."),
        R("BackOffice.OrganizationApiKeys.Create.ManagementDescription", "API key sadece yetkilendirilen kapsamlar için kullanılabilir. Oluşturulan gizli anahtar yalnızca bir kez gösterilir.", "The API key can only be used for authorized scopes. The generated secret key is shown only once."),
        R("BackOffice.OrganizationApiKeys.Create.FormTitle", "API Key Bilgileri", "API Key Information"),
        R("BackOffice.OrganizationApiKeys.Create.FormDescription", "Anahtar adı, ortamı, tipi, kısıtları ve yetki kapsamlarını girin.", "Enter the key name, environment, type, restrictions and permission scopes."),
        R("BackOffice.OrganizationApiKeys.ManagementDescription", "Bu ekrandan organizasyona ait API key kayıtlarını listeleyebilir ve yeni anahtar oluşturabilirsiniz.", "Use this page to list organization API keys and create a new key."),
        R("BackOffice.OrganizationApiKeys.OrganizationInactiveWarning", "Bu organizasyon pasif durumda. Pasif organizasyon için yeni API key oluşturulması önerilmez.", "This organization is inactive. Creating a new API key for an inactive organization is not recommended."),
        R("BackOffice.OrganizationApiKeys.OneTimeKey.Title", "API key oluşturuldu", "API key created"),
        R("BackOffice.OrganizationApiKeys.OneTimeKey.Warning", "Bu gizli anahtar yalnızca bir kez gösterilir. Lütfen güvenli bir yerde saklayın.", "This secret key is shown only once. Store it in a secure location."),
        R("BackOffice.OrganizationApiKeys.OneTimeKey.Value", "Gizli anahtar", "Secret key"),
        R("BackOffice.OrganizationApiKeys.NeverUsed", "Henüz kullanılmadı", "Never used"),
        R("BackOffice.OrganizationApiKeys.NotExpire", "Süresiz", "No expiration"),
        R("BackOffice.OrganizationApiKeys.Scopes.Empty", "Tanımlı yetki kapsamı bulunamadı.", "No permission scopes are defined."),
        R("BackOffice.OrganizationApiKeys.Scopes.EmptyShort", "Kapsam yok", "No scope"),
        R("BackOffice.OrganizationApiKeys.Scopes.ShowMore", "+{0} kapsam", "+{0} scopes"),
        R("BackOffice.OrganizationApiKeys.Validation.ScopeRequired", "En az bir yetki kapsamı seçmelisiniz.", "You must select at least one permission scope."),
        R("BackOffice.OrganizationApiKeys.Messages.KeyPrefixCopied", "Key prefix kopyalandı.", "Key prefix copied."),

        R("BackOffice.WorkflowTemplateTransitions.FromStatus", "Kaynak Durum", "Source Status"),
        R("BackOffice.WorkflowTemplateTransitions.ToStatus", "Hedef Durum", "Target Status"),
        R("BackOffice.WorkflowTemplateTransitions.HelpText", "Şablona eklenecek durum geçişini seçin. Geçiş, kaynak durumdan hedef duruma izin verilen hareketi temsil eder.", "Select the status transition to add to the template. The transition represents an allowed movement from source status to target status."),
        R("BackOffice.WorkflowTemplateTransitions.Validation.TransitionRequired", "Geçiş seçimi zorunludur.", "Transition selection is required.")
    };

    private static ResourceSeedDefinition R(string key, string tr, string en)
        => new("BackOffice", key, tr, en);
}
