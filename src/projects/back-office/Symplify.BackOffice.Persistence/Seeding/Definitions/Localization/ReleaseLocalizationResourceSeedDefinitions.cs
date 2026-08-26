namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class ReleaseLocalizationResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new("BackOffice.Congresses.Buttons", "BackOffice.Congresses.Buttons.SelectLogoDark", "Koyu Tema Logosu Seç", "Select Dark Theme Logo"),
        new("BackOffice.Congresses.Buttons", "BackOffice.Congresses.Buttons.SelectLogoLight", "Açık Tema Logosu Seç", "Select Light Theme Logo"),
        new("BackOffice.Congresses.Create", "BackOffice.Congresses.Create.OrganizationImagesDescription", "Organizasyon logoları kongre için varsayılan olarak kullanılabilir.", "Organization logos can be used as defaults for the congress."),
        new("BackOffice.Congresses.Create", "BackOffice.Congresses.Create.OrganizationImagesTitle", "Organizasyon Görselleri", "Organization Images"),
        new("BackOffice.Congresses.Edit", "BackOffice.Congresses.Edit.OrganizationImagesDescription", "Kongre için kullanılacak açık ve koyu tema logolarını güncelleyin.", "Update the light and dark theme logos used for this congress."),
        new("BackOffice.Congresses.Edit", "BackOffice.Congresses.Edit.OrganizationImagesTitle", "Kongre Görselleri", "Congress Images"),
        new("BackOffice.Congresses.Validation", "BackOffice.Congresses.Validation.CodeAlreadyExists", "Bu kongre kodu zaten kullanılıyor.", "This congress code is already in use."),
        new("BackOffice.Congresses.Validation", "BackOffice.Congresses.Validation.ContactEmailInvalid", "Kongre iletişim e-posta adresi geçerli olmalıdır.", "Congress contact email address must be valid."),
        new("BackOffice.Congresses.Validation", "BackOffice.Congresses.Validation.DefaultTranslationRequired", "Varsayılan dilde kongre başlığı zorunludur.", "Congress title is required in the default language."),
        new("BackOffice.Congresses.Validation", "BackOffice.Congresses.Validation.EntityNotFound", "Kongre kaydı bulunamadı.", "Congress record was not found."),
        new("BackOffice.Congresses.Validation", "BackOffice.Congresses.Validation.ObjectStorageBucketMissing", "Kongre görselleri için storage bucket yapılandırması bulunamadı.", "Storage bucket configuration for congress images was not found."),
        new("BackOffice.Congresses.Validation", "BackOffice.Congresses.Validation.OrganizationInactive", "Seçilen organizasyon aktif değil.", "The selected organization is not active."),
        new("BackOffice.Congresses.Validation", "BackOffice.Congresses.Validation.OrganizationNotFound", "Organizasyon kaydı bulunamadı.", "Organization record was not found."),
        new("BackOffice.Congresses.Validation", "BackOffice.Congresses.Validation.OrganizationShortNameRequired", "Organizasyon kısa adı zorunludur.", "Organization short name is required."),
        new("BackOffice.Congresses.Validation", "BackOffice.Congresses.Validation.SlugAlreadyExists", "Bu kongre slug değeri zaten kullanılıyor.", "This congress slug is already in use."),
        new("BackOffice.Congresses.Validation", "BackOffice.Congresses.Validation.TranslationTitleRequired", "Çeviri başlığı zorunludur.", "Translation title is required."),
        new("BackOffice.Organizations.DeleteConfirmText", "BackOffice.Organizations.DeleteConfirmText", "Bu organizasyonu silmek istediğinize emin misiniz?", "Are you sure you want to delete this organization?"),
        new("BackOffice.Organizations.DeleteConfirmTextWithName", "BackOffice.Organizations.DeleteConfirmTextWithName", "{0} organizasyonunu silmek istediğinize emin misiniz?", "Are you sure you want to delete organization {0}?"),
        new("BackOffice.Organizations.DeleteConfirmTitle", "BackOffice.Organizations.DeleteConfirmTitle", "Organizasyonu Sil", "Delete Organization"),
        new("BackOffice.Organizations.Validation", "BackOffice.Organizations.Validation.ObjectStorageBucketMissing", "Organizasyon görselleri için storage bucket yapılandırması bulunamadı.", "Storage bucket configuration for organization images was not found."),
        new("BackOffice.Sidebar.Definitions", "BackOffice.Sidebar.Definitions", "Tanımlar", "Definitions"),
        new("BackOffice.Topics.Messages", "BackOffice.Topics.Messages.NotFound", "Konu kaydı bulunamadı.", "Topic record was not found."),
        new("Common", "Common.ActiveQuestion", "Aktif mi?", "Active?"),
        new("Common", "Common.All", "Tümü", "All"),
        new("Common", "Common.FileTooLarge", "Dosya boyutu izin verilen sınırı aşıyor.", "File size exceeds the allowed limit."),
        new("Common", "Common.Filters", "Filtreler", "Filters"),
        new("Common", "Common.Hidden", "Gizli", "Hidden"),
        new("Common", "Common.NotSpecified", "Belirtilmemiş", "Not specified"),
        new("Common", "Common.Saved", "Kaydedildi.", "Saved."),
        new("Common", "Common.Visible", "Görünür", "Visible")
    };
}
