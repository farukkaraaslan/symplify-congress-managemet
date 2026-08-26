using System.Collections.Generic;

namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class CongressSliderResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new("BackOffice", "BackOffice.CongressSliders.ListTitle", "Slider Listesi", "Slider List"),
        new("BackOffice", "BackOffice.CongressSliders.ListDescription", "Kongre ana sayfasında gösterilecek slider kayıtları.", "Slider records displayed on the congress homepage."),
        new("BackOffice", "BackOffice.CongressSliders.BasicInfo", "Temel Bilgiler", "Basic Information"),
        new("BackOffice", "BackOffice.CongressSliders.Translations", "Çeviriler", "Translations"),

        new("BackOffice", "BackOffice.CongressSliders.Create.Title", "Yeni Slider", "New Slider"),
        new("BackOffice", "BackOffice.CongressSliders.Create.Description", "Slider ana bilgileri ve çevirileri tek seferde kaydedilir.", "Slider core information and translations are saved together."),
        new("BackOffice", "BackOffice.CongressSliders.Update.Title", "Slider Düzenle", "Edit Slider"),
        new("BackOffice", "BackOffice.CongressSliders.Update.Description", "Seçili slider kaydının temel bilgileri ve çevirileri güncellenir.", "Update the selected slider record and translations."),

        new("BackOffice", "BackOffice.CongressSliders.Fields.Order", "Sıra No", "Order"),
        new("BackOffice", "BackOffice.CongressSliders.Fields.Image", "Görsel", "Image"),
        new("BackOffice", "BackOffice.CongressSliders.Fields.CurrentImage", "Mevcut Görsel", "Current Image"),
        new("BackOffice", "BackOffice.CongressSliders.Fields.NewImage", "Yeni Görsel", "New Image"),
        new("BackOffice", "BackOffice.CongressSliders.Fields.Title", "Başlık", "Title"),
        new("BackOffice", "BackOffice.CongressSliders.Fields.Subtitle", "Açıklama", "Description"),
        new("BackOffice", "BackOffice.CongressSliders.Fields.ButtonText", "Buton Metni", "Button Text"),
        new("BackOffice", "BackOffice.CongressSliders.Fields.ButtonUrl", "Buton URL", "Button URL"),
        new("BackOffice", "BackOffice.CongressSliders.Fields.Language", "Dil", "Language"),
        new("BackOffice", "BackOffice.CongressSliders.Fields.Status", "Durum", "Status"),
        new("BackOffice", "BackOffice.CongressSliders.Fields.IsActive", "Aktif mi?", "Is Active?"),

        new("BackOffice", "BackOffice.CongressSliders.Buttons.New", "Yeni Slider", "New Slider"),
        new("BackOffice", "BackOffice.CongressSliders.Buttons.Save", "Sliderı Kaydet", "Save Slider"),
        new("BackOffice", "BackOffice.CongressSliders.Buttons.Update", "Sliderı Güncelle", "Update Slider"),
        new("BackOffice", "BackOffice.CongressSliders.Buttons.SelectImage", "Görsel Seç", "Choose Image"),
        new("BackOffice", "BackOffice.CongressSliders.Buttons.ChangeImage", "Görsel Değiştir", "Change Image"),

        new("BackOffice", "BackOffice.CongressSliders.Help.Image", "Web banner ölçüsünde JPG, PNG veya WEBP görsel önerilir. Maksimum 5 MB.", "JPG, PNG or WEBP image in web banner size is recommended. Maximum 5 MB."),
        new("BackOffice", "BackOffice.CongressSliders.Help.UpdateImage", "Mevcut görsel korunur; yeni görsel seçerseniz güncellenir.", "Current image is kept unless you choose a new image."),

        new("BackOffice", "BackOffice.CongressSliders.Messages.Created", "Slider başarıyla oluşturuldu.", "Slider created successfully."),
        new("BackOffice", "BackOffice.CongressSliders.Messages.Updated", "Slider başarıyla güncellendi.", "Slider updated successfully."),
        new("BackOffice", "BackOffice.CongressSliders.Messages.Deleted", "Slider başarıyla silindi.", "Slider deleted successfully."),

        new("BackOffice", "BackOffice.CongressSliders.Validation.CongressRequired", "Kongre bilgisi zorunludur.", "Congress is required."),
        new("BackOffice", "BackOffice.CongressSliders.Validation.ImageRequired", "Slider görseli zorunludur.", "Slider image is required."),
        new("BackOffice", "BackOffice.CongressSliders.Validation.ImageExtensionInvalid", "Sadece JPG, PNG veya WEBP görsel yükleyebilirsiniz.", "Only JPG, PNG or WEBP images are allowed."),
        new("BackOffice", "BackOffice.CongressSliders.Validation.ImageSizeInvalid", "Slider görseli en fazla 5 MB olabilir.", "Slider image can be at most 5 MB."),
        new("BackOffice", "BackOffice.CongressSliders.Validation.OrderInvalid", "Sıralama değeri sıfırdan küçük olamaz.", "Order cannot be lower than zero."),
        new("BackOffice", "BackOffice.CongressSliders.Validation.TitleRequired", "Varsayılan dilde slider başlığı zorunludur.", "Slider title is required in the default language."),
        new("BackOffice", "BackOffice.CongressSliders.Validation.TranslationTitleRequired", "Bu dil için herhangi bir içerik girildiyse slider başlığı da zorunludur.", "If any content is entered for this language, slider title is also required."),
        new("BackOffice", "BackOffice.CongressSliders.Validation.TitleMaxLengthExceeded", "Slider başlığı en fazla 300 karakter olabilir.", "Slider title can be at most 300 characters."),
        new("BackOffice", "BackOffice.CongressSliders.Validation.SubtitleMaxLengthExceeded", "Slider alt başlığı en fazla 1000 karakter olabilir.", "Slider subtitle can be at most 1000 characters."),
        new("BackOffice", "BackOffice.CongressSliders.Validation.ButtonTextMaxLengthExceeded", "Buton metni en fazla 120 karakter olabilir.", "Button text can be at most 120 characters."),
        new("BackOffice", "BackOffice.CongressSliders.Validation.ButtonUrlMaxLengthExceeded", "Buton URL değeri en fazla 1000 karakter olabilir.", "Button URL can be at most 1000 characters."),

        new("BackOffice", "BackOffice.CongressSliders.Js.active", "Aktif", "Active"),
        new("BackOffice", "BackOffice.CongressSliders.Js.passive", "Pasif", "Passive"),
        new("BackOffice", "BackOffice.CongressSliders.Js.edit", "Düzenle", "Edit"),
        new("BackOffice", "BackOffice.CongressSliders.Js.delete", "Sil", "Delete"),
        new("BackOffice", "BackOffice.CongressSliders.Js.fallback", "Fallback", "Fallback"),
        new("BackOffice", "BackOffice.CongressSliders.Js.saved", "Kayıt kaydedildi.", "Record saved."),
        new("BackOffice", "BackOffice.CongressSliders.Js.deleted", "Kayıt silindi.", "Record deleted."),
        new("BackOffice", "BackOffice.CongressSliders.Js.genericError", "İşlem sırasında bir hata oluştu.", "An error occurred during the operation."),
        new("BackOffice", "BackOffice.CongressSliders.Js.deleteConfirmTitle", "Emin misiniz?", "Are you sure?"),
        new("BackOffice", "BackOffice.CongressSliders.Js.deleteConfirmText", "Bu slider silinecek.", "This slider will be deleted."),
        new("BackOffice", "BackOffice.CongressSliders.Js.deleteConfirmButton", "Sil", "Delete"),
        new("BackOffice", "BackOffice.CongressSliders.Js.imageHelp", "Web banner ölçüsünde görsel önerilir.", "A web banner image is recommended."),
        new("BackOffice", "BackOffice.CongressSliders.Js.search", "Ara:", "Search:"),
        new("BackOffice", "BackOffice.CongressSliders.Js.lengthMenu", "_MENU_ kayıt göster", "Show _MENU_ entries"),
        new("BackOffice", "BackOffice.CongressSliders.Js.info", "_TOTAL_ kayıttan _START_ - _END_ arası gösteriliyor", "Showing _START_ to _END_ of _TOTAL_ entries"),
        new("BackOffice", "BackOffice.CongressSliders.Js.infoEmpty", "Kayıt bulunamadı", "No records found"),
        new("BackOffice", "BackOffice.CongressSliders.Js.zeroRecords", "Eşleşen kayıt bulunamadı", "No matching records found"),
        new("BackOffice", "BackOffice.CongressSliders.Js.first", "İlk", "First"),
        new("BackOffice", "BackOffice.CongressSliders.Js.last", "Son", "Last"),
        new("BackOffice", "BackOffice.CongressSliders.Js.next", "Sonraki", "Next"),
        new("BackOffice", "BackOffice.CongressSliders.Js.previous", "Önceki", "Previous")
    };
}
