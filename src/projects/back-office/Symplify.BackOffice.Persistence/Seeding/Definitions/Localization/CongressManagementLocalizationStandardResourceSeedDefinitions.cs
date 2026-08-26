using System.Collections.Generic;

namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class CongressManagementLocalizationStandardResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        R("BackOffice.CongressPaymentPlans.Validation.CongressRequired", "Kongre bilgisi zorunludur.", "Congress is required."),
        R("BackOffice.CongressPaymentPlans.Validation.ValidFromInvalid", "Geçerlilik başlangıç tarihi geçerli değil.", "Valid from date is invalid."),
        R("BackOffice.CongressPaymentPlans.Validation.ValidUntilInvalid", "Geçerlilik bitiş tarihi geçerli değil.", "Valid until date is invalid."),
        R("BackOffice.CongressPaymentPlans.Validation.DueDateInvalid", "Son ödeme tarihi geçerli değil.", "Due date is invalid."),
        R("BackOffice.CongressTopics.Messages.Saved", "Kongre konu seçimleri kaydedildi.", "Congress topic selections were saved."),
        R("BackOffice.CongressSubmissionTypes.Messages.Saved", "Kongre bildiri türü seçimleri kaydedildi.", "Congress submission type selections were saved."),

        R("BackOffice.Congresses.Validation.TranslationNotFound", "Kongre çevirisi bulunamadı.", "Congress translation was not found."),
        R("BackOffice.Congresses.Validation.DefaultTranslationCannotBeDeleted", "Varsayılan dil çevirisi silinemez.", "Default language translation cannot be deleted."),
        R("BackOffice.Congresses.Validation.PublishDateRangeInvalid", "Yayın bitiş tarihi yayın başlangıç tarihinden önce olamaz.", "Publish end date cannot be earlier than publish start date."),

        R("BackOffice.CongressSliders.Reorder.Help", "Sıralamayı değiştirmek için satırları sürükleyip bırakabilirsiniz.", "Drag and drop rows to change the order."),
        R("BackOffice.CongressSliders.Validation.EntityNotFound", "Slider kaydı bulunamadı.", "Slider record was not found."),
        R("BackOffice.CongressSliders.Validation.CongressNotFound", "Kongre bulunamadı.", "Congress was not found."),
        R("BackOffice.CongressSliders.Validation.TranslationNotFound", "Slider çevirisi bulunamadı.", "Slider translation was not found."),
        R("BackOffice.CongressSliders.Validation.DefaultTranslationCannotBeDeleted", "Varsayılan dil çevirisi silinemez.", "Default language translation cannot be deleted."),
        R("BackOffice.CongressSliders.Validation.ImageInvalid", "Slider görseli PNG, JPG, WEBP veya SVG formatında ve en fazla 5 MB olmalıdır.", "Slider image must be PNG, JPG, WEBP or SVG and at most 5 MB."),
        R("BackOffice.CongressSliders.Validation.ObjectStorageBucketMissing", "Slider görseli için object storage bucket ayarı bulunamadı.", "Object storage bucket configuration for slider images was not found."),
        R("BackOffice.CongressSliders.Validation.ReorderRequired", "Sıralanacak slider kaydı bulunamadı.", "No slider record was found to reorder."),
        R("BackOffice.CongressSliders.Validation.InvalidReorderList", "Slider sıralama listesi geçersiz.", "Slider reorder list is invalid."),

        R("BackOffice.CongressBoards.ListTitle", "Kurul Türleri", "Committee Types"),
        R("BackOffice.CongressBoards.ListDescription", "Kongrede kullanılacak kurul türlerini ve dil bazlı adlarını yönetin.", "Manage committee types and localized names used in the congress."),
        R("BackOffice.CongressBoards.Buttons.New", "Yeni Kurul", "New Committee"),
        R("BackOffice.CongressBoards.Fields.Order", "Sıra No", "Order"),
        R("BackOffice.CongressBoards.Validation.EntityNotFound", "Kurul bulunamadı.", "Committee was not found."),
        R("BackOffice.CongressBoards.Validation.TranslationNotFound", "Kurul çevirisi bulunamadı.", "Committee translation was not found."),
        R("BackOffice.CongressBoards.Validation.DefaultTranslationRequired", "Varsayılan dilde kurul adı zorunludur.", "Committee name is required in the default language."),
        R("BackOffice.CongressBoards.Validation.DefaultTranslationCannotBeDeleted", "Varsayılan dil çevirisi silinemez.", "Default language translation cannot be deleted."),
        R("BackOffice.CongressBoards.Validation.BoardHasMembers", "Bu kurul türüne bağlı kurul üyeleri bulunduğu için silinemez.", "This committee type cannot be deleted because it has committee members."),

        R("BackOffice.CongressBoardMembers.Help.PhotoCurrent", "Mevcut fotoğraf korunur; yeni fotoğraf seçerseniz güncellenir.", "Current photo is kept unless you choose a new photo."),
        R("BackOffice.CongressBoardMembers.Business.TranslationNotFound", "Kurul üyesi çevirisi bulunamadı.", "Committee member translation was not found."),
        R("BackOffice.CongressBoardMembers.Business.DefaultTranslationCannotBeDeleted", "Varsayılan dil çevirisi silinemez.", "Default language translation cannot be deleted."),
        R("BackOffice.CongressBoardMembers.Storage.BucketMissing", "Kurul üyesi görselleri için object storage bucket ayarı bulunamadı.", "Object storage bucket configuration for committee member images was not found."),

        R("BackOffice.CongressPaymentPlans.Create.Title", "Yeni Ödeme Planı", "New Payment Plan"),
        R("BackOffice.CongressPaymentPlans.Create.Description", "Kongre ödeme planını ve dil bazlı görünen ad bilgilerini kaydedin.", "Save the congress payment plan and localized display name."),
        R("BackOffice.CongressPaymentPlans.Update.Title", "Ödeme Planı Düzenle", "Edit Payment Plan"),
        R("BackOffice.CongressPaymentPlans.Update.Description", "Seçili ödeme planının tutar, dönem, durum ve dil bazlı metin bilgilerini güncelleyin.", "Update the selected payment plan amount, period, status and localized texts."),
        R("BackOffice.CongressPaymentPlans.Buttons.New", "Yeni Ödeme Planı", "New Payment Plan"),
        R("BackOffice.CongressPaymentPlans.Buttons.Save", "Ödeme Planını Kaydet", "Save Payment Plan"),
        R("BackOffice.CongressPaymentPlans.Buttons.Update", "Ödeme Planını Güncelle", "Update Payment Plan"),
        R("BackOffice.CongressPaymentPlans.Fields.Order", "Sıra No", "Order"),
        R("BackOffice.CongressPaymentPlans.Placeholders.Name", "Örn: Erken Kayıt", "Example: Early Registration"),
        R("BackOffice.CongressPaymentPlans.Placeholders.Description", "Planla ilgili kısa açıklama girin.", "Enter a short description for the plan."),
        R("BackOffice.CongressPaymentPlans.Code.Help", "Boş bırakılırsa sistem tarafından benzersiz bir kod oluşturulur.", "If left empty, a unique code is generated by the system."),
        R("BackOffice.CongressPaymentPlans.Help", "Tutar, para birimi ve geçerlilik dönemini kontrol ederek ödeme planını yayınlayın.", "Review amount, currency and validity period before publishing the payment plan."),
        R("BackOffice.CongressPaymentPlans.Validation.AudienceTypeRequired", "Katılımcı tipi seçimi zorunludur.", "Audience type is required."),
        R("BackOffice.CongressPaymentPlans.Validation.PaymentCategoryRequired", "Ödeme kategorisi seçimi zorunludur.", "Payment category is required."),
        R("BackOffice.CongressPaymentPlans.Validation.AmountRequired", "Tutar bilgisi zorunludur.", "Amount is required."),
        R("BackOffice.CongressPaymentPlans.Validation.CurrencyRequired", "Para birimi seçimi zorunludur.", "Currency is required."),

        R("BackOffice.CongressTopics.ListTitle", "Seçili Konular", "Selected Topics"),
        R("BackOffice.CongressTopics.ListDescription", "Kongrede kullanılacak konuları genel Konular lookup listesinden seçin.", "Select topics used in this congress from the global Topics lookup list."),
        R("BackOffice.CongressTopics.Buttons.Manage", "Konuları Düzenle", "Manage Topics"),
        R("BackOffice.CongressTopics.Empty", "Bu kongre için henüz konu seçilmedi.", "No topic has been selected for this congress yet."),
        R("BackOffice.CongressTopics.Modal.Title", "Konuları Düzenle", "Manage Topics"),
        R("BackOffice.CongressTopics.Modal.Description", "Genel Konular listesindeki aktif kayıtları bu kongre için görünür veya pasif hale getirin.", "Enable or disable active records from the global Topics list for this congress."),
        R("BackOffice.CongressTopics.SelectionTitle", "Konu Seçimi", "Topic Selection"),
        R("BackOffice.CongressTopics.NoGlobalLookup", "Aktif konu lookup kaydı bulunamadı. Önce genel Konular ekranından konu tanımlayın.", "No active topic lookup record was found. Define topics in the global Topics screen first."),
        R("BackOffice.CongressTopics.Messages.Synced", "Kongre konu seçimleri güncellendi.", "Congress topic selections were updated."),
        R("BackOffice.CongressTopics.Validation.EntityNotFound", "Kongre konu kaydı bulunamadı.", "Congress topic record was not found."),
        R("BackOffice.CongressTopics.Validation.CongressRequired", "Kongre bilgisi zorunludur.", "Congress is required."),
        R("BackOffice.CongressTopics.Validation.CongressNotFound", "Kongre bulunamadı.", "Congress was not found."),
        R("BackOffice.CongressTopics.Validation.TopicRequired", "Konu bilgisi zorunludur.", "Topic is required."),
        R("BackOffice.CongressTopics.Validation.TopicNotFound", "Seçilen konu bulunamadı.", "Selected topic was not found."),
        R("BackOffice.CongressTopics.Validation.InvalidSelectionList", "Konu seçim listesi geçersiz.", "Topic selection list is invalid."),
        R("BackOffice.CongressTopics.Buttons.ManageCategories", "Kategorileri Yönet", "Manage Categories"),
        R("BackOffice.CongressTopics.Category.Modal.Title", "Konu Kategorileri", "Topic Categories"),
        R("BackOffice.CongressTopics.Category.Modal.Description", "Bu kongrede konu gruplaması kullanılacaksa kategorileri dil bazlı tanımlayın. Kategori kullanımı opsiyoneldir.", "Define localized categories if this congress groups topics. Categories are optional."),
        R("BackOffice.CongressTopics.Category.Buttons.New", "Yeni Kategori", "New Category"),
        R("BackOffice.CongressTopics.Category.Empty", "Bu kongrede henüz konu kategorisi tanımlanmadı.", "No topic category has been defined for this congress yet."),
        R("BackOffice.CongressTopics.Category.None", "Kategori Yok", "No Category"),
        R("BackOffice.CongressTopics.Category.Label", "Kategori", "Category"),
        R("BackOffice.CongressTopics.Category.Name", "Kategori Adı", "Category Name"),
        R("BackOffice.CongressTopics.Category.Order", "Sıra", "Order"),
        R("BackOffice.CongressTopics.Messages.CategoriesSaved", "Konu kategorileri kaydedildi.", "Topic categories were saved."),
        R("BackOffice.CongressTopics.Validation.CategoryNotFound", "Seçilen konu kategorisi bu kongre için bulunamadı.", "The selected topic category was not found for this congress."),
        R("BackOffice.CongressTopics.Validation.DefaultCategoryNameRequired", "Varsayılan dilde kategori adı zorunludur.", "Category name is required in the default language."),
        R("BackOffice.CongressTopics.Validation.CategoryNameTooLong", "Kategori adı en fazla 200 karakter olabilir.", "Category name can contain at most 200 characters."),

        R("BackOffice.CongressSubmissionTypes.ListTitle", "Seçili Bildiri Türleri", "Selected Submission Types"),
        R("BackOffice.CongressSubmissionTypes.ListDescription", "Kongrede kabul edilecek bildiri türlerini genel Bildiri Türleri lookup listesinden seçin.", "Select accepted submission types for this congress from the global Submission Types lookup list."),
        R("BackOffice.CongressSubmissionTypes.Buttons.Manage", "Bildiri Türlerini Düzenle", "Manage Submission Types"),
        R("BackOffice.CongressSubmissionTypes.Empty", "Bu kongre için henüz bildiri türü seçilmedi.", "No submission type has been selected for this congress yet."),
        R("BackOffice.CongressSubmissionTypes.Modal.Title", "Bildiri Türlerini Düzenle", "Manage Submission Types"),
        R("BackOffice.CongressSubmissionTypes.Modal.Description", "Genel Bildiri Türleri listesindeki aktif kayıtları bu kongre için görünür veya pasif hale getirin.", "Enable or disable active records from the global Submission Types list for this congress."),
        R("BackOffice.CongressSubmissionTypes.SelectionTitle", "Bildiri Türü Seçimi", "Submission Type Selection"),
        R("BackOffice.CongressSubmissionTypes.NoGlobalLookup", "Aktif bildiri türü lookup kaydı bulunamadı. Önce genel Bildiri Türleri ekranından kayıt tanımlayın.", "No active submission type lookup record was found. Define submission types in the global Submission Types screen first."),
        R("BackOffice.CongressSubmissionTypes.Messages.Synced", "Kongre bildiri türü seçimleri güncellendi.", "Congress submission type selections were updated."),
        R("BackOffice.CongressSubmissionTypes.Validation.EntityNotFound", "Kongre bildiri türü kaydı bulunamadı.", "Congress submission type record was not found."),
        R("BackOffice.CongressSubmissionTypes.Validation.CongressRequired", "Kongre bilgisi zorunludur.", "Congress is required."),
        R("BackOffice.CongressSubmissionTypes.Validation.CongressNotFound", "Kongre bulunamadı.", "Congress was not found."),
        R("BackOffice.CongressSubmissionTypes.Validation.SubmissionTypeRequired", "Bildiri türü bilgisi zorunludur.", "Submission type is required."),
        R("BackOffice.CongressSubmissionTypes.Validation.SubmissionTypeNotFound", "Seçilen bildiri türü bulunamadı.", "Selected submission type was not found."),
        R("BackOffice.CongressSubmissionTypes.Validation.InvalidSelectionList", "Bildiri türü seçim listesi geçersiz.", "Submission type selection list is invalid."),

        R("BackOffice.CongressEvaluationCriteria.Validation.EntityNotFound", "Kongre değerlendirme kriteri bulunamadı.", "Congress evaluation criterion was not found."),

        // Added by localization audit: statically used keys that previously had no seed definition.
        new("BackOffice.CongressSubmissionTypes", "BackOffice.CongressSubmissionTypes.Validation.DuplicateSelectionId", "Aynı bildiri türü seçim listesinde birden fazla kez bulunamaz.", "The same submission type cannot appear more than once in the selection list."),
        new("BackOffice.CongressSubmissionTypes", "BackOffice.CongressSubmissionTypes.Validation.InvalidSelectionId", "Bildiri türü seçim listesinde geçersiz bir kayıt bulunuyor.", "The submission type selection list contains an invalid record."),
        new("BackOffice.CongressTopics", "BackOffice.CongressTopics.Validation.DuplicateSelectionId", "Aynı konu seçim listesinde birden fazla kez bulunamaz.", "The same topic cannot appear more than once in the selection list."),
        new("BackOffice.CongressTopics", "BackOffice.CongressTopics.Validation.InvalidSelectionId", "Konu seçim listesinde geçersiz bir kayıt bulunuyor.", "The topic selection list contains an invalid record."),
    };

    private static ResourceSeedDefinition R(string key, string tr, string en)
    {
        return new ResourceSeedDefinition("BackOffice", key, tr, en);
    }
}
