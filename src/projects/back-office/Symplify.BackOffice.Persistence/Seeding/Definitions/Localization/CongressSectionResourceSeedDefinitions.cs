namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class CongressSectionResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new("BackOffice", "BackOffice.CongressSections.ListTitle", "Bölüm Listesi", "Section List"),
        new("BackOffice", "BackOffice.CongressSections.ListDescription", "Kongre sayfasında gösterilecek içerik bölümleri.", "Content sections displayed on the congress page."),
        new("BackOffice", "BackOffice.CongressSections.BasicInfo", "Temel Bilgiler", "Basic Information"),
        new("BackOffice", "BackOffice.CongressSections.Translations", "Çeviriler", "Translations"),

        new("BackOffice", "BackOffice.CongressSections.Create.Title", "Yeni Bölüm", "New Section"),
        new("BackOffice", "BackOffice.CongressSections.Create.Description", "Bölüm temel bilgileri ve çevirileri tek seferde kaydedilir.", "Section core information and translations are saved together."),
        new("BackOffice", "BackOffice.CongressSections.Update.Title", "Bölüm Düzenle", "Edit Section"),
        new("BackOffice", "BackOffice.CongressSections.Update.Description", "Seçili bölüm kaydının temel bilgileri ve çevirileri güncellenir.", "Update the selected section record and translations."),

        new("BackOffice", "BackOffice.CongressSections.Fields.Order", "Sıra No", "Order"),
        new("BackOffice", "BackOffice.CongressSections.Fields.BindingKey", "Bağlantı Anahtarı", "Binding Key"),
        new("BackOffice", "BackOffice.CongressSections.Fields.Title", "Başlık", "Title"),
        new("BackOffice", "BackOffice.CongressSections.Fields.Content", "İçerik", "Content"),
        new("BackOffice", "BackOffice.CongressSections.Fields.Language", "Dil", "Language"),
        new("BackOffice", "BackOffice.CongressSections.Fields.Status", "Durum", "Status"),
        new("BackOffice", "BackOffice.CongressSections.Fields.IsActive", "Aktif mi?", "Is Active?"),

        new("BackOffice", "BackOffice.CongressSections.Placeholders.BindingKey", "Örn: about, venue, program", "E.g. about, venue, program"),
        new("BackOffice", "BackOffice.CongressSections.Placeholders.Title", "Bölüm başlığı", "Section title"),
        new("BackOffice", "BackOffice.CongressSections.Placeholders.Content", "Bölüm içeriği", "Section content"),

        new("BackOffice", "BackOffice.CongressSections.Buttons.New", "Yeni Bölüm", "New Section"),
        new("BackOffice", "BackOffice.CongressSections.Buttons.Save", "Bölümü Kaydet", "Save Section"),
        new("BackOffice", "BackOffice.CongressSections.Buttons.Update", "Bölümü Güncelle", "Update Section"),

        new("BackOffice", "BackOffice.CongressSections.Help.BindingKey", "Portal tarafında bu bölümü tanımlamak için kullanılır. Aynı kongrede tekrar edemez.", "Used to identify this section on the portal side. It must be unique within the same congress."),
        new("BackOffice", "BackOffice.CongressSections.Reorder.Help", "Sıralamayı değiştirmek için satırları sürükleyip bırakabilirsiniz.", "Drag and drop rows to change the order."),

        new("BackOffice", "BackOffice.CongressSections.Messages.Created", "Bölüm başarıyla oluşturuldu.", "Section created successfully."),
        new("BackOffice", "BackOffice.CongressSections.Messages.Updated", "Bölüm başarıyla güncellendi.", "Section updated successfully."),
        new("BackOffice", "BackOffice.CongressSections.Messages.Deleted", "Bölüm başarıyla silindi.", "Section deleted successfully."),
        new("BackOffice", "BackOffice.CongressSections.Messages.Reordered", "Bölüm sıralaması güncellendi.", "Section order updated successfully."),

        new("BackOffice", "BackOffice.CongressSections.Validation.EntityNotFound", "Bölüm bulunamadı.", "Section was not found."),
        new("BackOffice", "BackOffice.CongressSections.Validation.CongressNotFound", "Kongre bulunamadı.", "Congress was not found."),
        new("BackOffice", "BackOffice.CongressSections.Validation.TranslationNotFound", "Bölüm çevirisi bulunamadı.", "Section translation was not found."),
        new("BackOffice", "BackOffice.CongressSections.Validation.CongressRequired", "Kongre bilgisi zorunludur.", "Congress is required."),
        new("BackOffice", "BackOffice.CongressSections.Validation.BindingKeyRequired", "Bağlantı anahtarı zorunludur.", "Binding key is required."),
        new("BackOffice", "BackOffice.CongressSections.Validation.BindingKeyTooLong", "Bağlantı anahtarı en fazla 100 karakter olabilir.", "Binding key can be at most 100 characters."),
        new("BackOffice", "BackOffice.CongressSections.Validation.BindingKeyAlreadyExists", "Bu bağlantı anahtarı aynı kongre için zaten kullanılıyor.", "This binding key is already used for the same congress."),
        new("BackOffice", "BackOffice.CongressSections.Validation.OrderInvalid", "Sıralama değeri sıfırdan küçük olamaz.", "Order cannot be lower than zero."),
        new("BackOffice", "BackOffice.CongressSections.Validation.ReorderRequired", "Sıralanacak bölüm bulunamadı.", "No section was found to reorder."),
        new("BackOffice", "BackOffice.CongressSections.Validation.InvalidReorderList", "Sıralama listesi geçersiz.", "Reorder list is invalid."),
        new("BackOffice", "BackOffice.CongressSections.Validation.TitleRequired", "Varsayılan dilde bölüm başlığı zorunludur.", "Section title is required in the default language."),
        new("BackOffice", "BackOffice.CongressSections.Validation.TranslationTitleRequired", "Bu dil için herhangi bir içerik girildiyse bölüm başlığı da zorunludur.", "If any content is entered for this language, section title is also required."),
        new("BackOffice", "BackOffice.CongressSections.Validation.DefaultTranslationCannotBeDeleted", "Varsayılan dil çevirisi silinemez.", "Default language translation cannot be deleted."),

        new("BackOffice", "BackOffice.CongressSections.Js.active", "Aktif", "Active"),
        new("BackOffice", "BackOffice.CongressSections.Js.passive", "Pasif", "Passive"),
        new("BackOffice", "BackOffice.CongressSections.Js.edit", "Düzenle", "Edit"),
        new("BackOffice", "BackOffice.CongressSections.Js.delete", "Sil", "Delete"),
        new("BackOffice", "BackOffice.CongressSections.Js.fallback", "Fallback", "Fallback"),
        new("BackOffice", "BackOffice.CongressSections.Js.saved", "Kayıt kaydedildi.", "Record saved."),
        new("BackOffice", "BackOffice.CongressSections.Js.deleted", "Kayıt silindi.", "Record deleted."),
        new("BackOffice", "BackOffice.CongressSections.Js.reordered", "Sıralama güncellendi.", "Order updated."),
        new("BackOffice", "BackOffice.CongressSections.Js.genericError", "İşlem sırasında bir hata oluştu.", "An error occurred during the operation."),
        new("BackOffice", "BackOffice.CongressSections.Js.deleteConfirmTitle", "Emin misiniz?", "Are you sure?"),
        new("BackOffice", "BackOffice.CongressSections.Js.deleteConfirmText", "Bu bölüm silinecek.", "This section will be deleted."),
        new("BackOffice", "BackOffice.CongressSections.Js.deleteConfirmButton", "Sil", "Delete"),
        new("BackOffice", "BackOffice.CongressSections.Js.dragHandle", "Sırayı değiştirmek için sürükleyin", "Drag to reorder"),
        new("BackOffice", "BackOffice.CongressSections.Js.reorderNotAllowed", "Sıralama yapmak için arama boş olmalı ve tablo Sıra No kolonuna göre artan sıralanmalıdır.", "To reorder, search must be empty and the table must be sorted by Order ascending."),
        new("BackOffice", "BackOffice.CongressSections.Js.reorderEndpointMissing", "Sıralama endpoint adresi bulunamadı.", "Reorder endpoint was not found.")
    };
}
