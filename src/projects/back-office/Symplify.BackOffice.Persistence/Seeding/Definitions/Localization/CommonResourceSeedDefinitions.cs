namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class CommonResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new("Common", "Common.Panel", "Panel", "Dashboard"),
        new("Common", "Common.Lookups", "Tanımlar", "Lookups"),
        new("Common", "Common.Translations", "Çeviriler", "Translations"),
        new("Common", "Common.BasicInformation", "Temel Bilgiler", "Basic Information"),

        new("Common", "Common.Name", "Ad", "Name"),
        new("Common", "Common.Description", "Açıklama", "Description"),
        new("Common", "Common.Status", "Durum", "Status"),
        new("Common", "Common.Order", "Sıra", "Order"),
        new("Common", "Common.Actions", "İşlemler", "Actions"),

        new("Common", "Common.Active", "Aktif", "Active"),
        new("Common", "Common.Passive", "Pasif", "Passive"),

        new("Common", "Common.Create", "Oluştur", "Create"),
        new("Common", "Common.Update", "Güncelle", "Update"),
        new("Common", "Common.Edit", "Düzenle", "Edit"),
        new("Common", "Common.Delete", "Sil", "Delete"),
        new("Common", "Common.Close", "Kapat", "Close"),
        new("Common", "Common.Save", "Kaydet", "Save"),
        new("Common", "Common.SaveChanges", "Değişiklikleri Kaydet", "Save Changes"),
        new("Common", "Common.Search", "Ara", "Search"),
        new("Common", "Common.Records", "kayıt", "records"),

        new("Common", "Common.Yes", "Evet", "Yes"),
        new("Common", "Common.No", "Hayır", "No"),
        new("Common", "Common.Ok", "Tamam", "OK"),
        new("Common", "Common.Cancel", "Vazgeç", "Cancel"),
        new("Common", "Common.Confirm", "Onayla", "Confirm"),

        // BackOffice JavaScript tarafı bu key'leri doğrudan kullanıyor.
        new("Common", "Common.Success", "Başarılı", "Success"),
        new("Common", "Common.Error", "Hata", "Error"),
        new("Common", "Common.Warning", "Uyarı", "Warning"),
        new("Common", "Common.Info", "Bilgi", "Information"),

        // Eski/yeni kullanım uyumluluğu için title alias key'leri korunur.
        new("Common", "Common.SuccessTitle", "Başarılı", "Success"),
        new("Common", "Common.ErrorTitle", "Hata", "Error"),
        new("Common", "Common.WarningTitle", "Uyarı", "Warning"),
        new("Common", "Common.InfoTitle", "Bilgi", "Information"),

        new("Common", "Common.GenericError", "İşlem sırasında bir hata oluştu.", "An error occurred during the operation."),
        new("Common", "Common.GenericSuccess", "İşlem başarıyla tamamlandı.", "The operation was completed successfully."),
        new("Common", "Common.FormInvalid", "Form bilgileri geçerli değil.", "The form information is invalid."),
        new("Common", "Common.InvalidRequest", "Geçersiz istek.", "Invalid request."),

        // CRUD success messages. Controller ve ajax tarafı bu key'leri response message olarak kullanıyor.
        new("Common", "Common.Created", "Kayıt oluşturuldu.", "Record created successfully."),
        new("Common", "Common.Updated", "Kayıt güncellendi.", "Record updated successfully."),
        new("Common", "Common.Deleted", "Kayıt silindi.", "Record deleted successfully."),

        // SweetAlert delete confirmation messages.
        new("Common", "Common.DeleteConfirmTitle", "Silmek istediğinize emin misiniz?", "Are you sure you want to delete?"),
        new("Common", "Common.DeleteConfirmText", "Bu kayıt silinecek. Silme sonrası aktif kayıtların sıra numarası otomatik düzenlenecek.", "This record will be deleted. The order of active records will be adjusted automatically after deletion."),
        new("Common", "Common.DeleteConfirmButton", "Sil", "Delete"),

        // Lookup ordering / drag-drop shared messages.
        new("Common", "Common.Reorder", "Sırayı değiştir", "Change order"),
        new("Common", "Common.DragToReorder", "Sırayı değiştirmek için sürükleyin", "Drag to reorder"),
        new("Common", "Common.ReorderSuccess", "Sıralama güncellendi.", "Order updated successfully."),
        new("Common", "Common.ReorderNotAllowedShort", "Sıralama için arama boşken Sıra kolonunu artan kullanın.", "Clear search and sort by Order ascending to reorder."),
        new("Common", "Common.ReorderNotAllowed", "Sıralama yapmak için arama boş olmalı ve tablo Sıra kolonuna göre artan sıralanmalıdır.", "To reorder, search must be empty and the table must be sorted by Order ascending."),
        new("Common", "Common.ReorderEndpointMissing", "Sıralama endpoint adresi bulunamadı.", "Reorder endpoint URL was not found.")
    };
}
