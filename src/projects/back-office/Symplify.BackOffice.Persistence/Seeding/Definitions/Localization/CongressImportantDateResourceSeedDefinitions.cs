using System.Collections.Generic;

namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class CongressImportantDateResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new("BackOffice", "BackOffice.CongressImportantDates.ListTitle", "Önemli Tarihler Listesi", "Important Date List"),
        new("BackOffice", "BackOffice.CongressImportantDates.ListDescription", "Kongre sayfasında gösterilecek önemli tarih kayıtları.", "Important date records displayed on the congress page."),
        new("BackOffice", "BackOffice.CongressImportantDates.BasicInfo", "Temel Bilgiler", "Basic Information"),
        new("BackOffice", "BackOffice.CongressImportantDates.Translations", "Çeviriler", "Translations"),

        new("BackOffice", "BackOffice.CongressImportantDates.Create.Title", "Yeni Önemli Tarih", "New Important Date"),
        new("BackOffice", "BackOffice.CongressImportantDates.Create.Description", "Önemli tarih temel bilgileri ve çevirileri tek seferde kaydedilir.", "Important date core information and translations are saved together."),
        new("BackOffice", "BackOffice.CongressImportantDates.Update.Title", "Önemli Tarih Düzenle", "Edit Important Date"),
        new("BackOffice", "BackOffice.CongressImportantDates.Update.Description", "Seçili önemli tarih kaydının temel bilgileri ve çevirileri güncellenir.", "Update the selected important date record and translations."),

        new("BackOffice", "BackOffice.CongressImportantDates.Fields.Order", "Sıra No", "Order"),
        new("BackOffice", "BackOffice.CongressImportantDates.Fields.StartDate", "Başlangıç Tarihi", "Start Date"),
        new("BackOffice", "BackOffice.CongressImportantDates.Fields.EndDate", "Bitiş Tarihi", "End Date"),
        new("BackOffice", "BackOffice.CongressImportantDates.Fields.Title", "Başlık", "Title"),
        new("BackOffice", "BackOffice.CongressImportantDates.Fields.Description", "Açıklama", "Description"),
        new("BackOffice", "BackOffice.CongressImportantDates.Fields.Language", "Dil", "Language"),
        new("BackOffice", "BackOffice.CongressImportantDates.Fields.Status", "Durum", "Status"),
        new("BackOffice", "BackOffice.CongressImportantDates.Fields.IsActive", "Aktif mi?", "Is Active?"),

        new("BackOffice", "BackOffice.CongressImportantDates.Placeholders.StartDate", "gg.aa.yyyy ss:dd", "dd.MM.yyyy HH:mm"),
        new("BackOffice", "BackOffice.CongressImportantDates.Placeholders.EndDate", "gg.aa.yyyy ss:dd", "dd.MM.yyyy HH:mm"),
        new("BackOffice", "BackOffice.CongressImportantDates.Placeholders.Title", "Tarih başlığı", "Important date title"),
        new("BackOffice", "BackOffice.CongressImportantDates.Placeholders.Description", "Kısa açıklama", "Short description"),

        new("BackOffice", "BackOffice.CongressImportantDates.Buttons.New", "Yeni Tarih", "New Date"),
        new("BackOffice", "BackOffice.CongressImportantDates.Buttons.Save", "Tarihi Kaydet", "Save Date"),
        new("BackOffice", "BackOffice.CongressImportantDates.Buttons.Update", "Tarihi Güncelle", "Update Date"),

        new("BackOffice", "BackOffice.CongressImportantDates.Reorder.Help", "Sıralamayı değiştirmek için satırları sürükleyip bırakabilirsiniz.", "Drag and drop rows to change the order."),

        new("BackOffice", "BackOffice.CongressImportantDates.Messages.Created", "Önemli tarih başarıyla oluşturuldu.", "Important date created successfully."),
        new("BackOffice", "BackOffice.CongressImportantDates.Messages.Updated", "Önemli tarih başarıyla güncellendi.", "Important date updated successfully."),
        new("BackOffice", "BackOffice.CongressImportantDates.Messages.Deleted", "Önemli tarih başarıyla silindi.", "Important date deleted successfully."),
        new("BackOffice", "BackOffice.CongressImportantDates.Messages.Reordered", "Önemli tarih sıralaması güncellendi.", "Important date order updated successfully."),

        new("BackOffice", "BackOffice.CongressImportantDates.Validation.EntityNotFound", "Önemli tarih bulunamadı.", "Important date was not found."),
        new("BackOffice", "BackOffice.CongressImportantDates.Validation.CongressNotFound", "Kongre bulunamadı.", "Congress was not found."),
        new("BackOffice", "BackOffice.CongressImportantDates.Validation.TranslationNotFound", "Önemli tarih çevirisi bulunamadı.", "Important date translation was not found."),
        new("BackOffice", "BackOffice.CongressImportantDates.Validation.CongressRequired", "Kongre bilgisi zorunludur.", "Congress is required."),
        new("BackOffice", "BackOffice.CongressImportantDates.Validation.StartDateRequired", "Başlangıç tarihi zorunludur.", "Start date is required."),
        new("BackOffice", "BackOffice.CongressImportantDates.Validation.EndDateRequired", "Bitiş tarihi zorunludur.", "End date is required."),
        new("BackOffice", "BackOffice.CongressImportantDates.Validation.DateRangeInvalid", "Bitiş tarihi başlangıç tarihinden önce olamaz.", "End date cannot be earlier than start date."),
        new("BackOffice", "BackOffice.CongressImportantDates.Validation.OrderInvalid", "Sıralama değeri sıfırdan küçük olamaz.", "Order cannot be lower than zero."),
        new("BackOffice", "BackOffice.CongressImportantDates.Validation.ReorderRequired", "Sıralanacak önemli tarih bulunamadı.", "No important date was found to reorder."),
        new("BackOffice", "BackOffice.CongressImportantDates.Validation.InvalidReorderList", "Sıralama listesi geçersiz.", "Reorder list is invalid."),
        new("BackOffice", "BackOffice.CongressImportantDates.Validation.TitleRequired", "Varsayılan dilde tarih başlığı zorunludur.", "Important date title is required in the default language."),
        new("BackOffice", "BackOffice.CongressImportantDates.Validation.TranslationTitleRequired", "Bu dil için herhangi bir açıklama girildiyse tarih başlığı da zorunludur.", "If any description is entered for this language, important date title is also required."),
        new("BackOffice", "BackOffice.CongressImportantDates.Validation.DefaultTranslationCannotBeDeleted", "Varsayılan dil çevirisi silinemez.", "Default language translation cannot be deleted."),

        new("BackOffice", "BackOffice.CongressImportantDates.Js.active", "Aktif", "Active"),
        new("BackOffice", "BackOffice.CongressImportantDates.Js.passive", "Pasif", "Passive"),
        new("BackOffice", "BackOffice.CongressImportantDates.Js.edit", "Düzenle", "Edit"),
        new("BackOffice", "BackOffice.CongressImportantDates.Js.delete", "Sil", "Delete"),
        new("BackOffice", "BackOffice.CongressImportantDates.Js.fallback", "Fallback", "Fallback"),
        new("BackOffice", "BackOffice.CongressImportantDates.Js.saved", "Kayıt kaydedildi.", "Record saved."),
        new("BackOffice", "BackOffice.CongressImportantDates.Js.deleted", "Kayıt silindi.", "Record deleted."),
        new("BackOffice", "BackOffice.CongressImportantDates.Js.reordered", "Sıralama güncellendi.", "Order updated."),
        new("BackOffice", "BackOffice.CongressImportantDates.Js.genericError", "İşlem sırasında bir hata oluştu.", "An error occurred during the operation."),
        new("BackOffice", "BackOffice.CongressImportantDates.Js.deleteConfirmTitle", "Emin misiniz?", "Are you sure?"),
        new("BackOffice", "BackOffice.CongressImportantDates.Js.deleteConfirmText", "Bu önemli tarih silinecek.", "This important date will be deleted."),
        new("BackOffice", "BackOffice.CongressImportantDates.Js.deleteConfirmButton", "Sil", "Delete"),
        new("BackOffice", "BackOffice.CongressImportantDates.Js.dragHandle", "Sırayı değiştirmek için sürükleyin", "Drag to reorder"),
        new("BackOffice", "BackOffice.CongressImportantDates.Js.reorderNotAllowed", "Sıralama yapmak için arama boş olmalı ve tablo Sıra No kolonuna göre artan sıralanmalıdır.", "To reorder, search must be empty and the table must be sorted by Order ascending."),
        new("BackOffice", "BackOffice.CongressImportantDates.Js.reorderEndpointMissing", "Sıralama endpoint adresi bulunamadı.", "Reorder endpoint was not found.")
    };
}
