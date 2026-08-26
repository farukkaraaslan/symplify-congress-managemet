using System.Collections.Generic;

namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class CongressPaymentPlanResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new("BackOffice", "BackOffice.CongressPaymentPlans.ListTitle", "Ödeme Planları", "Payment Plans"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.ListDescription", "Kongreye ait ödeme planlarını yönetin.", "Manage congress payment plans."),
        new("BackOffice", "BackOffice.CongressPaymentPlans.BasicInfo", "Temel Bilgiler", "Basic Information"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Translations", "Çeviriler", "Translations"),

        new("BackOffice", "BackOffice.CongressPaymentPlans.Fields.Code", "Kod", "Code"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Fields.Name", "Plan Adı", "Plan Name"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Fields.Description", "Açıklama", "Description"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Fields.Amount", "Tutar", "Amount"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Fields.Currency", "Para Birimi", "Currency"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Fields.AudienceType", "Katılımcı Tipi", "Audience Type"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Fields.PaymentCategory", "Ödeme Kategorisi", "Payment Category"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Fields.ValidFrom", "Geçerlilik Başlangıcı", "Valid From"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Fields.ValidUntil", "Geçerlilik Bitişi", "Valid Until"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Fields.DueDate", "Son Tarih", "Due Date"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Fields.IsPublicVisible", "Public alanda göster", "Show publicly"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Fields.IsActive", "Aktif mi?", "Is Active?"),

        new("BackOffice", "BackOffice.CongressPaymentPlans.Audience.All", "Tümü", "All"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Audience.Domestic", "Yerli Katılımcı", "Domestic Participant"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Audience.International", "Yabancı Katılımcı", "International Participant"),

        new("BackOffice", "BackOffice.CongressPaymentPlans.Category.Participation", "Katılım", "Participation"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Category.SecondSubmission", "İkinci Bildiri", "Second Submission"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Category.Listener", "Dinleyici", "Listener"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Category.Student", "Öğrenci", "Student"),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Category.Other", "Diğer", "Other"),

        new("BackOffice", "BackOffice.CongressPaymentPlans.Messages.Created", "Ödeme planı başarıyla oluşturuldu.", "Payment plan created successfully."),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Messages.Updated", "Ödeme planı başarıyla güncellendi.", "Payment plan updated successfully."),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Messages.Deleted", "Ödeme planı başarıyla silindi.", "Payment plan deleted successfully."),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Messages.Reordered", "Ödeme planı sıralaması güncellendi.", "Payment plan order updated successfully."),

        new("BackOffice", "BackOffice.CongressPaymentPlans.Business.EntityNotFound", "Ödeme planı bulunamadı.", "Payment plan was not found."),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Business.TranslationNotFound", "Ödeme planı çevirisi bulunamadı.", "Payment plan translation was not found."),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Business.DefaultTranslationRequired", "Varsayılan dilde plan adı zorunludur.", "Plan name is required in the default language."),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Business.DefaultTranslationCannotBeDeleted", "Varsayılan dil çevirisi silinemez.", "Default language translation cannot be deleted."),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Business.CodeAlreadyExists", "Bu kongrede aynı ödeme planı kodu zaten kullanılıyor.", "The payment plan code is already used in this congress."),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Business.InvalidAudienceType", "Katılımcı tipi geçersiz.", "Audience type is invalid."),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Business.InvalidPaymentCategory", "Ödeme kategorisi geçersiz.", "Payment category is invalid."),
        new("BackOffice", "BackOffice.CongressPaymentPlans.Business.InvalidDateRange", "Geçerlilik bitişi başlangıç tarihinden önce olamaz.", "Valid until cannot be earlier than valid from.")
    };
}
