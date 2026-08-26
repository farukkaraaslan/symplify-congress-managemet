namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

/// <summary>
/// Seed definitions for previously hardcoded Turkish/English strings that were converted to DB-driven localization keys.
/// </summary>
public static class HardcodedStringsLocalizationSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        // Common

        // Submission business rules

        // Submission form validation messages

        // Exhibition-specific validation messages (UpdateSubmission)

        // Submission reviewer business rules

        // SubmissionWorkflow validation

        // CongressTopics sync validation

        // CongressSubmissionTypes sync validation

        // Role administration

        // User administration

        // File upload (symplify.dropzone.js)

        // Submissions/Manage.cshtml

        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.AlertTitle", "Editör yönetim ekranı", "Editor management screen"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.AlertText", "Bildiri içeriğini inceleyebilir, DB tanımlı workflow geçişlerini çalıştırabilir, hakem/değerlendirme/history/kabul yazısı ve mail durumlarını görebilirsiniz.", "You can review submission content, run DB-defined workflow transitions, and view reviewer/evaluation/history/acceptance letter and mail statuses."),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.SubmissionInfo", "Bildiri Bilgileri", "Submission Info"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.SubmissionInfoSub", "Başvuruya ait temel kayıt bilgileri.", "Basic record information for the application."),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.SubmissionNo", "Bildiri No", "Submission No"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.SubmissionType", "Bildiri Türü", "Submission Type"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.Topic", "Konu", "Topic"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.PaymentStatus", "Ödeme Durumu", "Payment Status"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.Submission", "Gönderim", "Submission"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.Submitted", "Gönderildi", "Submitted"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.NotSubmitted", "Gönderilmedi", "Not Submitted"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.CreatedDate", "Oluşturma", "Created"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.SubmittedAt", "Gönderim Tarihi", "Submitted At"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.AverageScore", "Ortalama Puan", "Average Score"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.TitleKeywordsAbstract", "Başlık, Anahtar Kelimeler ve Özet", "Title, Keywords and Abstract"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.Title", "Başlık", "Title"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.Keywords", "Anahtar Kelimeler", "Keywords"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.Abstract", "Özet", "Abstract"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.TitleEn", "Title", "Title"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.KeywordsEn", "Keywords", "Keywords"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.AbstractEn", "Abstract", "Abstract"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.Authors", "Yazarlar", "Authors"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.NoAuthors", "Yazar kaydı bulunamadı.", "No author records found."),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.AuthorName", "Yazar", "Author"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.AuthorTitle", "Unvan", "Title"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.AuthorEmail", "E-posta", "Email"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.AuthorInstitution", "Kurum", "Institution"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.Corresponding", "Sorumlu", "Corresponding"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.Files", "Dosyalar", "Files"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.NoFiles", "Bildiri dosyası bulunamadı.", "No submission files found."),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.FileKind", "Tür", "Type"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.FileName", "Dosya", "File"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.FileSize", "Boyut", "Size"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.UploadedAt", "Yükleme", "Uploaded At"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.ReviewersEvaluations", "Hakemler ve Değerlendirmeler", "Reviewers and Evaluations"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.NoReviewers", "Henüz hakem atanmadı.", "No reviewers assigned yet."),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.Recommendation", "Öneri", "Recommendation"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.Score", "Puan", "Score"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.ScoreCount", "Skor Sayısı", "Score Count"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.ProcessHistory", "Süreç Geçmişi", "Process History"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.NoHistory", "Süreç geçmişi bulunamadı.", "No process history found."),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.Automatic", "Otomatik", "Automatic"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.PerformedBy", "İşlem yapan", "Performed by"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.PublicNote", "Yazara not", "Note to author"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.InternalNote", "İç not", "Internal note"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.ProcessSummary", "Süreç Özeti", "Process Summary"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.SubmissionStatus", "Bildiri Durumu", "Submission Status"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.TransitionCount", "Geçiş Sayısı", "Transition Count"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.ReviewerCount", "Hakem Sayısı", "Reviewer Count"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.EvaluationCount", "Değerlendirme Sayısı", "Evaluation Count"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.WorkflowActions", "Workflow İşlemleri", "Workflow Actions"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.WorkflowActionsSub", "Butonlar DB'deki aktif transition kayıtlarından gelir.", "Buttons are generated from active transition records in the DB."),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.NoTransitions", "Bu durum için tanımlı geçiş bulunamadı.", "No transitions defined for this status."),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.PublicNotePlaceholder", "Yazara görünecek not", "Note visible to author"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.InternalNotePlaceholder", "İç not", "Internal note"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.AcceptanceLetters", "Kabul Yazıları", "Acceptance Letters"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.NoAcceptanceLetters", "Kabul yazısı oluşturulmadı.", "No acceptance letters generated."),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.GeneratedAt", "Oluşturma", "Generated At"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.SentAt", "Gönderim", "Sent At"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.ViewPdf", "PDF Görüntüle", "View PDF"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.MailQueue", "Mail Kuyruğu", "Mail Queue"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.NoMailMessages", "Mail kuyruğu kaydı bulunamadı.", "No mail queue records found."),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.Recipient", "Alıcı", "Recipient"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.Attempt", "Deneme", "Attempt"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.PaymentDocuments", "Ödeme Belgeleri", "Payment Documents"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.NoPaymentDocuments", "Ödeme belgesi bulunamadı.", "No payment documents found."),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.Approved", "Onaylı", "Approved"),
        new("BackOffice.Submissions", "BackOffice.Submissions.Manage.Pending", "Bekliyor", "Pending"),

        // Users views
        new("BackOffice.Users", "BackOffice.Users.PageTitle", "Kullanıcılar", "Users"),
        new("BackOffice.Users", "BackOffice.Users.PageSubtitle", "BackOffice kullanıcılarını, rollerini ve güvenlik durumlarını yönetin.", "Manage BackOffice users, roles and security statuses."),
        new("BackOffice.Users", "BackOffice.Users.NewUser", "Yeni Kullanıcı", "New User"),
        new("BackOffice.Users", "BackOffice.Users.SearchPlaceholder", "Ad, soyad, e-posta, kurum veya ORCID", "Name, surname, email, institution or ORCID"),
        new("BackOffice.Users", "BackOffice.Users.Organization", "Organizasyon", "Organization"),
        new("BackOffice.Users", "BackOffice.Users.AllOrganizations", "Tüm organizasyonlar", "All organizations"),
        new("BackOffice.Users", "BackOffice.Users.Blacklist", "Kara liste", "Blacklist"),
        new("BackOffice.Users", "BackOffice.Users.All", "Tümü", "All"),
        new("BackOffice.Users", "BackOffice.Users.NormalUsers", "Normal kullanıcılar", "Normal users"),
        new("BackOffice.Users", "BackOffice.Users.BlacklistedUsers", "Kara listedekiler", "Blacklisted users"),
        new("BackOffice.Users", "BackOffice.Users.UserList", "Kullanıcı Listesi", "User List"),
        new("BackOffice.Users", "BackOffice.Users.UserListSubtitle", "Merkezi DataTables ile sayfalama, sıralama, arama ve organizasyon filtresi uygulanır.", "Pagination, sorting, search and organization filter are applied via central DataTables."),
        new("BackOffice.Users", "BackOffice.Users.Manage", "Yönet", "Manage"),
        new("BackOffice.Users", "BackOffice.Users.Locked", "Kilitli", "Locked"),
        new("BackOffice.Users", "BackOffice.Users.BlacklistBadge", "Kara Liste", "Blacklisted"),
        new("BackOffice.Users", "BackOffice.Users.EmailUnconfirmed", "E-posta Onaysız", "Email Unconfirmed"),
        new("BackOffice.Users", "BackOffice.Users.ColumnUser", "Kullanıcı", "User"),
        new("BackOffice.Users", "BackOffice.Users.ColumnInstitution", "Kurum", "Institution"),
        new("BackOffice.Users", "BackOffice.Users.ColumnRoles", "Roller", "Roles"),
        new("BackOffice.Users", "BackOffice.Users.ColumnCreated", "Oluşturma", "Created"),
        new("BackOffice.Users", "BackOffice.Users.Create.PageTitle", "Yeni Kullanıcı", "New User"),
        new("BackOffice.Users", "BackOffice.Users.Create.PageSubtitle", "BackOffice kullanıcısı oluşturun ve başlangıç rollerini seçin.", "Create a BackOffice user and select initial roles."),
        new("BackOffice.Users", "BackOffice.Users.Surname", "Soyad", "Surname"),
        new("BackOffice.Users", "BackOffice.Users.Phone", "Telefon", "Phone"),
        new("BackOffice.Users", "BackOffice.Users.Institution", "Kurum", "Institution"),
        new("BackOffice.Users", "BackOffice.Users.CreateEmailConfirmed", "E-posta onaylı oluştur", "Create with confirmed email"),
        new("BackOffice.Users", "BackOffice.Users.GeneratePassword", "Güvenli rastgele şifre oluştur", "Generate secure random password"),
        new("BackOffice.Users", "BackOffice.Users.ManualPasswordPlaceholder", "Manuel şifre için doldurun", "Fill in for manual password"),
        new("BackOffice.Users", "BackOffice.Users.GeneratePasswordHint", "Rastgele şifre seçiliyse bu alan dikkate alınmaz.", "This field is ignored when random password is selected."),
        new("BackOffice.Users", "BackOffice.Users.CreateSubmit", "Kullanıcı Oluştur", "Create User"),
        new("BackOffice.Users", "BackOffice.Users.Edit.PageTitle", "Kullanıcı Düzenle", "Edit User"),
        new("BackOffice.Users", "BackOffice.Users.Edit.PageSubtitle", "Kullanıcının profil, lokasyon ve kongre erişim bilgilerini güncelleyin.", "Update the user's profile, location and congress access information."),
        new("BackOffice.Users", "BackOffice.Users.BackToDetail", "Detaya Dön", "Back to Detail"),
        new("BackOffice.Users", "BackOffice.Users.Edit.ProfileSection", "Profil Bilgileri", "Profile Information"),
        new("BackOffice.Users", "BackOffice.Users.Edit.ProfileSectionSubtitle", "Kimlik, iletişim ve akademik profil alanları.", "Identity, communication and academic profile fields."),
        new("BackOffice.Users", "BackOffice.Users.Title", "Unvan", "Title"),
        new("BackOffice.Users", "BackOffice.Users.SelectTitle", "Unvan seçiniz", "Select title"),
        new("BackOffice.Users", "BackOffice.Users.Edit.LocationSection", "Lokasyon Bilgileri", "Location Information"),
        new("BackOffice.Users", "BackOffice.Users.Edit.LocationSectionSubtitle", "Kullanıcının ülke ve il/eyalet bilgisi.", "User's country and state/province information."),
        new("BackOffice.Users", "BackOffice.Users.Country", "Ülke", "Country"),
        new("BackOffice.Users", "BackOffice.Users.SelectCountry", "Ülke seçiniz", "Select country"),
        new("BackOffice.Users", "BackOffice.Users.State", "İl / Eyalet", "State / Province"),
        new("BackOffice.Users", "BackOffice.Users.SelectState", "İl / eyalet seçiniz", "Select state / province"),
        new("BackOffice.Users", "BackOffice.Users.Edit.OrgSection", "Organizasyon ve Kongre Erişimi", "Organization and Congress Access"),
        new("BackOffice.Users", "BackOffice.Users.Edit.OrgSectionSubtitle", "Kullanıcının aktif organizasyon bağlamı ve varsayılan kongresi.", "User's active organization context and default congress."),
        new("BackOffice.Users", "BackOffice.Users.SelectOrganization", "Organizasyon seçiniz", "Select organization"),
        new("BackOffice.Users", "BackOffice.Users.DefaultCongress", "Varsayılan Kongre", "Default Congress"),
        new("BackOffice.Users", "BackOffice.Users.SelectDefaultCongress", "Varsayılan kongre seçiniz", "Select default congress"),
        new("BackOffice.Users", "BackOffice.Users.OrgAccessActive", "Organizasyon erişimi aktif", "Organization access active"),
        new("BackOffice.Users", "BackOffice.Users.Edit.AccountSection", "Hesap Durumu", "Account Status"),
        new("BackOffice.Users", "BackOffice.Users.Edit.AccountSectionSubtitle", "Kimlik doğrulama ve hesap kilitleme ayarları.", "Authentication and account lockout settings."),
        new("BackOffice.Users", "BackOffice.Users.EmailConfirmed", "E-posta onaylı", "Email confirmed"),
        new("BackOffice.Users", "BackOffice.Users.LockoutEnabled", "Hesap kilitleme aktif", "Account lockout enabled"),
        new("BackOffice.Users", "BackOffice.Users.Details.PageTitle", "Kullanıcı Detayı", "User Detail"),
        new("BackOffice.Users", "BackOffice.Users.TempPassword", "Geçici şifre:", "Temporary password:"),
        new("BackOffice.Users", "BackOffice.Users.TempPasswordHint", "Bu şifreyi güvenli bir kanaldan kullanıcıya iletin. Bu bilgi sadece bu ekranda gösterilir.", "Share this password with the user via a secure channel. This information is only shown on this screen."),
        new("BackOffice.Users", "BackOffice.Users.Details.ProfileCard", "Profil ve Güvenlik", "Profile and Security"),
        new("BackOffice.Users", "BackOffice.Users.FullName", "Ad Soyad", "Full Name"),
        new("BackOffice.Users", "BackOffice.Users.EmailConfirmedBadge", "E-posta Onaylı", "Email Confirmed"),
        new("BackOffice.Users", "BackOffice.Users.Details.PasswordCard", "Şifre İşlemleri", "Password Operations"),
        new("BackOffice.Users", "BackOffice.Users.Details.PasswordCardHint", "Sistem güvenli rastgele şifre üretir. Aynı kullanıcı için 1 saat içinde en fazla 4 kez sıfırlama yapılabilir.", "The system generates a secure random password. Password can be reset at most 4 times per hour per user."),
        new("BackOffice.Users", "BackOffice.Users.ResetPasswordConfirmTitle", "Şifre sıfırlansın mı?", "Reset password?"),
        new("BackOffice.Users", "BackOffice.Users.ResetPasswordConfirmText", "Yeni geçici şifre üretilecek ve sadece bir kez gösterilecek.", "A new temporary password will be generated and shown only once."),
        new("BackOffice.Users", "BackOffice.Users.ResetPasswordBtn", "Rastgele Şifre Üret ve Sıfırla", "Generate Random Password and Reset"),
        new("BackOffice.Users", "BackOffice.Users.Details.BlacklistCardHint", "Kara listeye alınan kullanıcı giriş yapamaz ve hesabı uzun süreli kilitlenir.", "A blacklisted user cannot log in and their account will be locked for a long period."),
        new("BackOffice.Users", "BackOffice.Users.BlacklistConfirmTitle", "Kara liste durumu değişsin mi?", "Change blacklist status?"),
        new("BackOffice.Users", "BackOffice.Users.BlacklistConfirmText", "Bu işlem kullanıcının sisteme erişimini etkiler.", "This action will affect the user's access to the system."),
        new("BackOffice.Users", "BackOffice.Users.RemoveFromBlacklist", "Kara Listeden Çıkar", "Remove from Blacklist"),
        new("BackOffice.Users", "BackOffice.Users.AddToBlacklist", "Kara Listeye Al", "Add to Blacklist"),
        new("BackOffice.Users", "BackOffice.Users.Details.DeactivateCard", "Kullanıcıyı Pasife Al", "Deactivate User"),
        new("BackOffice.Users", "BackOffice.Users.Details.DeactivateCardHint", "Bu işlem kullanıcıyı soft-delete yapar, kara listeye alır ve girişini kapatır.", "This action soft-deletes the user, adds them to the blacklist, and disables login."),
        new("BackOffice.Users", "BackOffice.Users.DeactivateConfirmTitle", "Kullanıcı pasife alınsın mı?", "Deactivate user?"),
        new("BackOffice.Users", "BackOffice.Users.DeactivateConfirmText", "Kullanıcı listeden kaldırılacak ve giriş yapamayacak.", "The user will be removed from the list and will not be able to log in."),
        new("BackOffice.Users", "BackOffice.Users.Details.DeactivateBtn", "Kullanıcıyı Pasife Al", "Deactivate User"),
        new("BackOffice.Users", "BackOffice.Users.Details.RoleCard", "Rol Atama", "Role Assignment"),
        new("BackOffice.Users", "BackOffice.Users.SaveRoles", "Rolleri Kaydet", "Save Roles"),
        new("BackOffice.Users", "BackOffice.Users.Details.ClaimsCard", "Doğrudan Yetki / Claim Atama", "Direct Permission / Claim Assignment"),
        new("BackOffice.Users", "BackOffice.Users.Details.ClaimsCardSubtitle", "Rol dışında kullanıcıya özel permission claim tanımlayın.", "Define user-specific permission claims outside of roles."),
        new("BackOffice.Users", "BackOffice.Users.SaveClaims", "Yetkileri Kaydet", "Save Permissions"),

        // Roles views
        new("BackOffice.Roles", "BackOffice.Roles.PageTitle", "Roller", "Roles"),
        new("BackOffice.Roles", "BackOffice.Roles.PageSubtitle", "Dinamik rol ve rol yetkilerini yönetin.", "Manage dynamic roles and role permissions."),
        new("BackOffice.Roles", "BackOffice.Roles.NewRole", "Yeni Rol", "New Role"),
        new("BackOffice.Roles", "BackOffice.Roles.SearchPlaceholder", "Rol adı veya açıklama", "Role name or description"),
        new("BackOffice.Roles", "BackOffice.Roles.Filter", "Filtrele", "Filter"),
        new("BackOffice.Roles", "BackOffice.Roles.ColumnRole", "Rol", "Role"),
        new("BackOffice.Roles", "BackOffice.Roles.ColumnUserCount", "Kullanıcı", "Users"),
        new("BackOffice.Roles", "BackOffice.Roles.ColumnClaimCount", "Yetki", "Permissions"),
        new("BackOffice.Roles", "BackOffice.Roles.ColumnCreated", "Oluşturma", "Created"),
        new("BackOffice.Roles", "BackOffice.Roles.NotFound", "Rol bulunamadı.", "No roles found."),
        new("BackOffice.Roles", "BackOffice.Roles.Create.PageTitle", "Rol Oluştur", "Create Role"),
        new("BackOffice.Roles", "BackOffice.Roles.Create.PageSubtitle", "Yeni dinamik rol oluşturun.", "Create a new dynamic role."),
        new("BackOffice.Roles", "BackOffice.Roles.RoleName", "Rol Adı", "Role Name"),
        new("BackOffice.Roles", "BackOffice.Roles.Edit.PageTitle", "Rol Güncelle", "Update Role"),
        new("BackOffice.Roles", "BackOffice.Roles.Edit.PageSubtitle", "Rol adını ve açıklamasını güncelleyin.", "Update role name and description."),
        new("BackOffice.Roles", "BackOffice.Roles.BackToDetail", "Detaya Dön", "Back to Detail"),
        new("BackOffice.Roles", "BackOffice.Roles.Details.PageTitle", "Rol Detayı", "Role Detail"),
        new("BackOffice.Roles", "BackOffice.Roles.Details.PageSubtitle", "Rol yetkilerini claim bazlı yönetin.", "Manage role permissions based on claims."),
        new("BackOffice.Roles", "BackOffice.Roles.Details.InfoCard", "Rol Bilgisi", "Role Information"),
        new("BackOffice.Roles", "BackOffice.Roles.Details.AssignedClaims", "Atanmış Yetki", "Assigned Permissions"),
        new("BackOffice.Roles", "BackOffice.Roles.Details.ClaimsCard", "Rol Yetkileri", "Role Permissions"),
        new("BackOffice.Roles", "BackOffice.Roles.Details.ClaimsCardSubtitle", "Bu rolün görebileceği menü ve çalıştırabileceği action'lar buradan belirlenir.", "The menus visible to this role and the actions it can execute are defined here."),
        new("BackOffice.Roles", "BackOffice.Roles.SaveClaims", "Yetkileri Kaydet", "Save Permissions"),

        // Organizations/_DeleteModal.cshtml
        new("BackOffice.Organizations", "BackOffice.Organizations.DeleteModal.Title", "Organizasyon Sil", "Delete Organization"),
        new("BackOffice.Organizations", "BackOffice.Organizations.DeleteModal.Confirm", "Bu organizasyonu silmek istediğine emin misin?", "Are you sure you want to delete this organization?"),

        // Reviewers/Users.cshtml
        new("BackOffice.Reviewers", "BackOffice.Reviewers.Users.PageTitle", "Hakem Ekle", "Add Reviewer"),
        new("BackOffice.Reviewers", "BackOffice.Reviewers.Users.Subtitle", "Kullanıcı listesinde arama yapın ve uygun kullanıcıyı hakem havuzuna ekleyin.", "Search the user list and add the appropriate user to the reviewer pool."),
        new("BackOffice.Reviewers", "BackOffice.Reviewers.Users.BackToReviewers", "Hakemlere Dön", "Back to Reviewers"),
        new("BackOffice.Reviewers", "BackOffice.Reviewers.Users.SearchLabel", "Hakem yapılacak kullanıcıyı ara", "Search for user to make reviewer"),
        new("BackOffice.Reviewers", "BackOffice.Reviewers.Users.ResultsTitle", "Kullanıcı Arama Sonuçları", "User Search Results"),
        new("BackOffice.Reviewers", "BackOffice.Reviewers.Users.ResultsSubtitle", "Hakemlik kullanıcı hesabı üzerinden açılır. Bu sayfa menüde ayrı görünmez; Hakem Havuzu içindeki Hakem Ekle akışıdır.", "Reviewer status is managed through the user account. This page is not visible in the menu; it is part of the Add Reviewer flow within the Reviewer Pool."),
        new("BackOffice.Reviewers", "BackOffice.Reviewers.Users.ColUser", "Kullanıcı", "User"),
        new("BackOffice.Reviewers", "BackOffice.Reviewers.Users.ColReviewerStatus", "Hakem Durumu", "Reviewer Status"),
        new("BackOffice.Reviewers", "BackOffice.Reviewers.Users.NoUsers", "Kullanıcı bulunamadı.", "No users found."),
        new("BackOffice.Reviewers", "BackOffice.Reviewers.Users.IsReviewer", "Hakem", "Reviewer"),
        new("BackOffice.Reviewers", "BackOffice.Reviewers.Users.Blacklisted", "Kara Liste", "Blacklisted"),
        new("BackOffice.Reviewers", "BackOffice.Reviewers.Users.NotReviewer", "Hakem değil", "Not a reviewer"),
        new("BackOffice.Reviewers", "BackOffice.Reviewers.Users.MakeReviewer", "Hakem Yap", "Make Reviewer"),
        new("BackOffice.Reviewers", "BackOffice.Reviewers.Users.ReviewerDetails", "Hakem Detayı", "Reviewer Details"),

        // Phone validation (auth-pages.js)

    };
}
