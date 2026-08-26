namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class MailResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        R("BackOffice.Mail.Common.Footer", "Bu e-posta Symplify üzerinden otomatik olarak gönderilmiştir.", "This email was sent automatically by Symplify."),
        R("BackOffice.Mail.Common.IfNotRequested", "Bu işlemi siz başlatmadıysanız bu e-postayı dikkate almayabilirsiniz.", "If you did not request this action, you can safely ignore this email."),
        R("BackOffice.Mail.Common.OpenInBrowserFallback", "Buton çalışmazsa aşağıdaki bağlantıyı tarayıcınıza kopyalayabilirsiniz.", "If the button does not work, copy the link below into your browser."),

        R("BackOffice.Mail.Auth.EmailConfirmation.Subject", "E-posta adresinizi doğrulayın", "Confirm your email address"),
        R("BackOffice.Mail.Auth.EmailConfirmation.Title", "E-posta adresinizi doğrulayın", "Confirm your email address"),
        R("BackOffice.Mail.Auth.EmailConfirmation.Body", "Merhaba {{RecipientName}},\nHesabınızı kullanmaya başlamak için e-posta adresinizi doğrulamanız gerekir.", "Hello {{RecipientName}},\nPlease confirm your email address to start using your account."),
        R("BackOffice.Mail.Auth.EmailConfirmation.Button", "E-postamı Doğrula", "Confirm My Email"),

        R("BackOffice.Mail.Auth.ResetPassword.Subject", "Şifrenizi sıfırlayın", "Reset your password"),
        R("BackOffice.Mail.Auth.ResetPassword.Title", "Şifrenizi sıfırlayın", "Reset your password"),
        R("BackOffice.Mail.Auth.ResetPassword.Body", "Merhaba {{RecipientName}},\nHesabınız için şifre sıfırlama talebi aldık. Yeni şifrenizi belirlemek için aşağıdaki butonu kullanabilirsiniz.", "Hello {{RecipientName}},\nWe received a password reset request for your account. Use the button below to set a new password."),
        R("BackOffice.Mail.Auth.ResetPassword.Button", "Şifremi Sıfırla", "Reset My Password"),

        R("BackOffice.Mail.Submission.Accepted.Subject", "Kabul mektubunuz hazır - {{SubmissionNumber}}", "Your acceptance letter is ready - {{SubmissionNumber}}"),
        R("BackOffice.Mail.Submission.Accepted.Title", "Kabul mektubunuz hazır", "Your acceptance letter is ready"),
        R("BackOffice.Mail.Submission.Accepted.Body", "Sayın {{RecipientName}},\n{{SubmissionTitle}} başlıklı bildiriniz kabul edilmiştir. Kişiye özel kabul mektubunuzu aşağıdaki buton üzerinden güvenli bağlantı ile görüntüleyebilirsiniz.", "Dear {{RecipientName}},\nYour submission titled {{SubmissionTitle}} has been accepted. You can securely view your personalized acceptance letter using the button below."),
        R("BackOffice.Mail.Submission.Accepted.Button", "Kabul Mektubunu Görüntüle", "View Acceptance Letter"),

        R("BackOffice.Mail.Submission.SentToReview.Subject", "Bildiriniz değerlendirme sürecine alındı - {{SubmissionNumber}}", "Your submission is under review - {{SubmissionNumber}}"),
        R("BackOffice.Mail.Submission.SentToReview.Title", "Bildiriniz hakem değerlendirme sürecine alındı", "Your submission is under review"),
        R("BackOffice.Mail.Submission.SentToReview.Body", "Sayın {{RecipientName}},\n{{SubmissionTitle}} başlıklı bildiriniz hakem değerlendirme sürecine alınmıştır. Sürecin ilerleyişini sistem üzerinden takip edebilirsiniz.", "Dear {{RecipientName}},\nYour submission titled {{SubmissionTitle}} has been moved to the reviewer evaluation process. You can follow the progress through the system."),
        R("BackOffice.Mail.Submission.SentToReview.Button", "Bildiri Detayını Görüntüle", "View Submission Details"),

        R("BackOffice.Mail.Submission.PaymentPending.Subject", "Bildiriniz için ödeme süreci başladı - {{SubmissionNumber}}", "Payment process started for your submission - {{SubmissionNumber}}"),
        R("BackOffice.Mail.Submission.PaymentPending.Title", "Ödeme belgesi bekleniyor", "Payment document is expected"),
        R("BackOffice.Mail.Submission.PaymentPending.Body", "Sayın {{RecipientName}},\n{{SubmissionTitle}} başlıklı bildiriniz kabul edilmiştir ve ödeme süreci başlamıştır. Ödemenizi ilgili IBAN hesabına yaptıktan sonra sistemde bildiri detayına girerek ödeme belgenizi yükleyebilirsiniz. Dekont yükleme imkânınız yoksa bu alanı dikkate almayabilirsiniz; ödemeniz tarafımıza ulaştığında editör/yönetici tarafından manuel olarak onaylanacaktır.", "Dear {{RecipientName}},\nYour submission titled {{SubmissionTitle}} has been accepted and the payment process has started. After completing the payment to the relevant IBAN account, you can upload your payment document from the submission details page. If you cannot upload a receipt, you may ignore that step; once your payment is received, it will be manually approved by the editor/administrator."),
        R("BackOffice.Mail.Submission.PaymentPending.Button", "Ödeme Belgesi Yükle", "Upload Payment Document"),

        R("BackOffice.Mail.Submission.PaymentApproved.Subject", "Ödemeniz onaylandı - {{SubmissionNumber}}", "Your payment has been approved - {{SubmissionNumber}}"),
        R("BackOffice.Mail.Submission.PaymentApproved.Title", "Ödeme işleminiz tamamlandı", "Your payment has been completed"),
        R("BackOffice.Mail.Submission.PaymentApproved.Body", "Sayın {{RecipientName}},\n{{SubmissionTitle}} başlıklı bildiriniz için ödeme işlemi onaylanmıştır. Süreciniz ödeme açısından tamamlanmıştır. Bildiri detaylarınızı sistem üzerinden görüntüleyebilirsiniz.", "Dear {{RecipientName}},\nThe payment for your submission titled {{SubmissionTitle}} has been approved. Your payment process has been completed. You can view your submission details through the system."),
        R("BackOffice.Mail.Submission.PaymentApproved.Button", "Bildiri Detayını Görüntüle", "View Submission Details"),

        R("BackOffice.Mail.Submission.Label.SubmissionNumber", "Bildiri No", "Submission Number"),
        R("BackOffice.Mail.Submission.Label.SubmissionTitle", "Bildiri Başlığı", "Submission Title"),
        R("BackOffice.Mail.Submission.Label.Congress", "Kongre", "Congress"),
        R("BackOffice.Mail.Submission.Label.LetterNumber", "Kabul Belgesi No", "Acceptance Letter Number")
    };

    private static ResourceSeedDefinition R(string key, string tr, string en)
    {
        return new ResourceSeedDefinition("BackOffice.Mail", key, tr, en);
    }
}
