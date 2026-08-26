namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class MailDeliveryResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        R("BackOffice.Sidebar", "BackOffice.Sidebar.MailDeliveries", "E-posta Gönderimleri", "Email Deliveries"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Title", "E-posta Gönderimleri", "Email Deliveries"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Description", "Uygulamadan çıkan tüm e-postaların kuyruk, SMTP ve AWS SES teslimat durumlarını tek ekrandan izleyin.", "Monitor queue, SMTP and AWS SES delivery status for every email sent by the application."),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Records", "Gönderim Kayıtları", "Delivery Records"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Empty", "Filtrelere uygun e-posta gönderimi bulunamadı.", "No email delivery matched the filters."),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Note.Title", "Durum farkı:", "Status distinction:"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Note.Body", "Gönderildi, Symplify'ın mesajı SMTP/AWS SES'e başarıyla verdiğini; Teslim Edildi ise SES'in alıcı posta sunucusundan başarılı teslim yanıtı aldığını ifade eder.", "Sent means Symplify handed the message to SMTP/AWS SES successfully; Delivered means SES received a successful delivery response from the recipient mail server."),

        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Summary.Total", "Toplam", "Total"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Summary.TransportPending", "Kuyruk / Gönderiliyor", "Queued / Processing"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Summary.Delivered", "Teslim Edildi", "Delivered"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Summary.Bounced", "Bounce", "Bounced"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Summary.TransportFailed", "Gönderim Hatası", "Transport Failed"),

        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Filters.Search", "Alıcı / E-posta / Konu / Bildiri No", "Recipient / Email / Subject / Submission No"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Filters.Organization", "Organizasyon", "Organization"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Filters.Congress", "Kongre", "Congress"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Filters.MailType", "Mail Türü", "Email Type"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Filters.TransportStatus", "Gönderim Durumu", "Transport Status"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Filters.DeliveryStatus", "Teslimat Durumu", "Delivery Status"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Filters.DateFrom", "Başlangıç", "From"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Filters.DateTo", "Bitiş", "To"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Filters.Apply", "Filtrele", "Filter"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Filters.Clear", "Temizle", "Clear"),

        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Columns.CreatedAt", "Tarih", "Date"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Columns.Type", "Tür", "Type"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Columns.Recipient", "Alıcı", "Recipient"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Columns.Context", "Organizasyon / Kongre", "Organization / Congress"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Columns.Subject", "Konu", "Subject"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Columns.Transport", "Gönderim", "Transport"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Columns.Delivery", "Teslimat", "Delivery"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Pagination.Summary", "Sayfa {0} / {1} · Toplam {2} kayıt", "Page {0} / {1} · {2} records"),

        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.MailType.Unknown", "Bilinmiyor", "Unknown"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.MailType.EmailConfirmation", "E-posta Doğrulama", "Email Confirmation"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.MailType.PasswordReset", "Şifre Sıfırlama", "Password Reset"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.MailType.OrganizationMailTest", "Mail Ayarı Testi", "Mail Configuration Test"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.MailType.SubmissionSentToReview", "Hakem Süreci", "Sent to Review"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.MailType.SubmissionPaymentPending", "Ödeme Bekleniyor", "Payment Pending"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.MailType.SubmissionPaymentApproved", "Ödeme Onaylandı", "Payment Approved"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.MailType.SubmissionAccepted", "Bildiri Kabul", "Submission Accepted"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.MailType.AcceptanceLetter", "Kabul Belgesi", "Acceptance Letter"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.MailType.ParticipationCertificate", "Katılım Belgesi", "Participation Certificate"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.MailType.BulkEmail", "Toplu E-posta", "Bulk Email"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.MailType.OtherSystem", "Sistem E-postası", "System Email"),

        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Transport.Pending", "Kuyrukta", "Queued"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Transport.Sent", "Gönderildi", "Sent"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Transport.Failed", "Başarısız", "Failed"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Transport.Cancelled", "İptal", "Cancelled"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Transport.Processing", "Gönderiliyor", "Processing"),

        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Delivery.Unknown", "Bilinmiyor", "Unknown"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Delivery.NotTracked", "Takip Edilmiyor", "Not Tracked"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Delivery.Pending", "SES Bekleniyor", "Awaiting SES"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Delivery.Delivered", "Teslim Edildi", "Delivered"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Delivery.Delayed", "Gecikiyor", "Delayed"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Delivery.Bounced", "Bounce", "Bounced"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Delivery.Rejected", "Reddedildi", "Rejected"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Delivery.Complaint", "Şikayet", "Complaint"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Delivery.RenderingFailed", "İçerik Hatası", "Rendering Failed"),

        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.Title", "E-posta Gönderim Detayı", "Email Delivery Detail"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.Message", "Mesaj", "Message"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.Recipient", "Alıcı", "Recipient"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.Submission", "Bildiri", "Submission"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.AcceptanceLetter", "Kabul Belgesi", "Acceptance Letter"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.Certificate", "Katılım Belgesi", "Participation Certificate"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.Delivery", "Gönderim / Teslimat", "Transport / Delivery"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.TransportStatus", "Gönderim", "Transport"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.DeliveryStatus", "Teslimat", "Delivery"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.Provider", "Sağlayıcı", "Provider"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.CreatedAt", "Kuyruğa Alınma", "Queued At"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.SentAt", "Gönderilme", "Sent At"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.DeliveredAt", "Teslim", "Delivered At"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.Attempts", "Deneme", "Attempts"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.Error", "Hata / Sağlayıcı Cevabı", "Error / Provider Response"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.Timeline", "SES Olay Geçmişi", "SES Event Timeline"),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.NoEvents", "Henüz sağlayıcı teslimat olayı alınmadı.", "No provider delivery event has been received yet."),
        R("BackOffice.MailDeliveries", "BackOffice.MailDeliveries.Detail.ProviderResponse", "Sağlayıcı Cevabı", "Provider Response")
    };

    private static ResourceSeedDefinition R(string area, string key, string tr, string en)
        => new(area, key, tr, en);
}
