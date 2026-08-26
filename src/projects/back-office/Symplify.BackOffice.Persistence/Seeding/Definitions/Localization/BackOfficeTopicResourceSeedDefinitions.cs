namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class BackOfficeTopicResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new("BackOffice.Topics", "BackOffice.Topics.PageTitle", "Konular", "Topics"),
        new("BackOffice.Topics", "BackOffice.Topics.PageDescription", "Kongrelerde kullanılacak konu başlıklarını yönetin.", "Manage topic headings used in congresses."),
        new("BackOffice.Topics", "BackOffice.Topics.ManagementTitle", "Konular yönetimi", "Topic management"),
        new("BackOffice.Topics", "BackOffice.Topics.ManagementDescription", "Kayıtları listeleyebilir, yeni kayıt oluşturabilir veya mevcut kayıtları düzenleyebilirsiniz. Çeviri alanları dil sekmeleriyle yönetilir.", "You can list records, create new records, or update existing records. Translation fields are managed through language tabs."),
        new("BackOffice.Topics", "BackOffice.Topics.ListTitle", "Konular Listesi", "Topic List"),
        new("BackOffice.Topics", "BackOffice.Topics.ListDescription", "Aktif/pasif kayıtları görüntüleyin ve düzenleyin.", "View and edit active/passive records."),
        new("BackOffice.Topics", "BackOffice.Topics.CreateButton", "Yeni Konu", "New Topic"),

        new("BackOffice.Topics", "BackOffice.Topics.CreateModalTitle", "Konu Oluştur", "Create Topic"),
        new("BackOffice.Topics", "BackOffice.Topics.UpdateModalTitle", "Konu Düzenle", "Edit Topic"),
        new("BackOffice.Topics", "BackOffice.Topics.TranslationModalDescription", "Çeviri bilgilerini dil sekmelerinden yönetebilirsiniz.", "You can manage translation information through language tabs."),

        new("BackOffice.Topics", "BackOffice.Topics.Fields.Name", "Konu Adı", "Topic Name"),
        new("BackOffice.Topics", "BackOffice.Topics.Fields.Description", "Açıklama", "Description"),
        new("BackOffice.Topics", "BackOffice.Topics.Placeholders.Name", "Konu adını giriniz", "Enter topic name"),
        new("BackOffice.Topics", "BackOffice.Topics.Placeholders.Description", "Konu açıklaması giriniz", "Enter topic description"),

        new("BackOffice.Topics.Validation", "BackOffice.Topics.Validation.EntityNotFound", "Konu bulunamadı.", "Topic was not found."),
        new("BackOffice.Topics.Validation", "BackOffice.Topics.Validation.TranslationNotFound", "Konu çevirisi bulunamadı.", "Topic translation was not found."),
        new("BackOffice.Topics.Validation", "BackOffice.Topics.Validation.DefaultTranslationRequired", "Varsayılan dilde konu adı zorunludur.", "Topic name is required in the default language."),
        new("BackOffice.Topics.Validation", "BackOffice.Topics.Validation.DefaultTranslationCannotBeDeleted", "Varsayılan dil çevirisi silinemez.", "The default language translation cannot be deleted."),
        new("BackOffice.Topics.Validation", "BackOffice.Topics.Validation.AtLeastOneTranslationRequired", "En az bir dil için konu adı girilmelidir.", "A topic name must be entered for at least one language."),
        new("BackOffice.Topics.Validation", "BackOffice.Topics.Validation.LanguageRequired", "Dil bilgisi zorunludur.", "Language is required."),
        new("BackOffice.Topics.Validation", "BackOffice.Topics.Validation.NameMaxLength", "Konu adı en fazla 250 karakter olabilir.", "Topic name can be at most 250 characters."),
        new("BackOffice.Topics.Validation", "BackOffice.Topics.Validation.DescriptionMaxLength", "Açıklama en fazla 1000 karakter olabilir.", "Description can be at most 1000 characters."),
        new("BackOffice.Topics.Validation", "BackOffice.Topics.Validation.NameAlreadyExists", "Bu dil için aynı konu adı zaten kayıtlı.", "A topic with the same name already exists for this language."),
        new("BackOffice.Topics.Validation", "BackOffice.Topics.Validation.DuplicateTranslationNameInRequest", "Aynı dil için aynı konu adı birden fazla kez girilemez.", "The same topic name cannot be entered more than once for the same language."),

        new("BackOffice.Topics.Messages", "BackOffice.Topics.Messages.Created", "Konu başarıyla oluşturuldu.", "Topic was created successfully."),
        new("BackOffice.Topics.Messages", "BackOffice.Topics.Messages.Updated", "Konu başarıyla güncellendi.", "Topic was updated successfully."),
        new("BackOffice.Topics.Messages", "BackOffice.Topics.Messages.Deleted", "Konu başarıyla silindi.", "Topic was deleted successfully."),
        new("BackOffice.Topics.Messages", "BackOffice.Topics.Messages.DeleteConfirm", "Bu kaydı silmek istediğinize emin misiniz?", "Are you sure you want to delete this record?"),
        new("BackOffice.Topics.Messages", "BackOffice.Topics.Messages.InvalidTopic", "Geçersiz konu bilgisi.", "Invalid topic information.")
    };
}
