namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

/// <summary>
/// Role-neutral BackOffice Home page localization resources.
/// </summary>
public static class HomePageResourceSeedDefinitions
{
    private static ResourceSeedDefinition R(string area, string key, string tr, string en)
        => new(area, key, tr, en);

    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        R("BackOffice.Home", "BackOffice.Home.PageTitle", "Ana Sayfa", "Home"),
        R("BackOffice.Home", "BackOffice.Home.DefaultUserName", "Kullanıcı", "User"),

        R("BackOffice.Home", "BackOffice.Home.HeroBadge", "Symplify", "Symplify"),
        R("BackOffice.Home", "BackOffice.Home.HeroTitle", "Çalışma alanına hoş geldin", "Welcome to your workspace"),
        R("BackOffice.Home", "BackOffice.Home.HeroTitleWithName", "Hoş geldin, {0}", "Welcome, {0}"),
        R("BackOffice.Home", "BackOffice.Home.HeroSubtitle", "Bildirilerinizi ve size tanımlanan işlemleri sol menüden takip edebilirsiniz.", "You can track your submissions and assigned actions from the left menu."),

        R("BackOffice.Home", "BackOffice.Home.Author.Badge", "Yazar Bildiri Paneli", "Author Submission Panel"),
        R("BackOffice.Home", "BackOffice.Home.Author.WelcomeTitle", "Hoş geldiniz", "Welcome"),
        R("BackOffice.Home", "BackOffice.Home.Author.Description", "Sözlü bildiri, poster bildiri veya sergi bildirisi oluşturabilir; bildirilerinizi ve belgelerinizi aynı panelden takip edebilirsiniz.", "You can create an oral submission, poster submission, or exhibition submission and track your submissions and documents from the same panel."),
        R("BackOffice.Home", "BackOffice.Home.Author.MySubmissionsButton", "Bildirilerimi Gör", "View My Submissions"),
        R("BackOffice.Home", "BackOffice.Home.Author.ActiveCongress.Title", "Aktif Kongre", "Active Congress"),
        R("BackOffice.Home", "BackOffice.Home.Author.ActiveCongress.Empty", "Aktif kongre bilgisi bulunamadı", "Active congress information was not found"),
        R("BackOffice.Home", "BackOffice.Home.Author.ActiveCongress.VenueFallback", "Online / Ankara", "Online / Ankara"),
        R("BackOffice.Home", "BackOffice.Home.Author.ActiveCongress.LanguageSupport", "Türkçe ve İngilizce destekli", "Turkish and English supported"),

        R("BackOffice.AuthorTopbar", "BackOffice.AuthorTopbar.Home", "Panel", "Dashboard"),
        R("BackOffice.AuthorTopbar", "BackOffice.AuthorTopbar.MySubmissions", "Bildirilerim", "My Submissions"),
        R("BackOffice.AuthorTopbar", "BackOffice.AuthorTopbar.ThemeToggle", "Tema değiştir", "Toggle theme"),
        R("BackOffice.AuthorTopbar", "BackOffice.AuthorTopbar.Notifications", "Bildirimler", "Notifications"),

        R("BackOffice.UserMenu", "BackOffice.UserMenu.AriaLabel", "Kullanıcı menüsü", "User menu"),
        R("BackOffice.UserMenu", "BackOffice.UserMenu.Profile", "Profilim", "My Profile"),
        R("BackOffice.UserMenu", "BackOffice.UserMenu.ChangePassword", "Şifremi Değiştir", "Change Password"),
        R("BackOffice.UserMenu", "BackOffice.UserMenu.Settings", "Ayarlar", "Settings"),
        R("BackOffice.UserMenu", "BackOffice.UserMenu.Logout", "Çıkış Yap", "Logout"),

        R("BackOffice.LanguageSwitcher", "BackOffice.LanguageSwitcher.Title", "Dil Seçimi", "Language Selection"),
        R("BackOffice.LanguageSwitcher", "BackOffice.LanguageSwitcher.Description", "Varsayılan dil listenin en başında gösterilir.", "The default language is shown at the top of the list."),
        R("BackOffice.LanguageSwitcher", "BackOffice.LanguageSwitcher.Default", "Varsayılan", "Default"),

        R("BackOffice.Home", "BackOffice.Home.Actions.MySubmissions", "Bildirilerim", "My Submissions"),
        R("BackOffice.Home", "BackOffice.Home.Actions.MyReviews", "Değerlendirmelerim", "My Reviews"),

        R("BackOffice.Home", "BackOffice.Home.UserBox.Label", "Aktif kullanıcı", "Active user"),
        R("BackOffice.Home", "BackOffice.Home.UserBox.Description", "Görebileceğin menüler ve işlem adımları rol ve yetkilerine göre belirlenir.", "The menus and actions you can access are determined by your roles and permissions."),

        R("BackOffice.Home", "BackOffice.Home.Cards.Submission.Tag", "Bildiri", "Submission"),
        R("BackOffice.Home", "BackOffice.Home.Cards.Submission.Title", "Bildirilerim", "My Submissions"),
        R("BackOffice.Home", "BackOffice.Home.Cards.Submission.Description", "Gönderdiğin bildirileri, dosyaları, revizyon taleplerini ve karar durumlarını bu alandan takip edebilirsin.", "You can track your submitted papers, files, revision requests and decision statuses from this area."),
        R("BackOffice.Home", "BackOffice.Home.Cards.Submission.Link", "Bildirilerime git", "Go to my submissions"),

        R("BackOffice.Home", "BackOffice.Home.Cards.Reviewer.Tag", "Değerlendirme", "Review"),
        R("BackOffice.Home", "BackOffice.Home.Cards.Reviewer.Title", "Hakem Değerlendirmeleri", "Reviewer Evaluations"),
        R("BackOffice.Home", "BackOffice.Home.Cards.Reviewer.Description", "Hakem olarak atanmışsan bekleyen, devam eden ve tamamlanan değerlendirme süreçlerini buradan izleyebilirsin.", "If you are assigned as a reviewer, you can track pending, ongoing and completed review processes here."),
        R("BackOffice.Home", "BackOffice.Home.Cards.Reviewer.Link", "Değerlendirmelere git", "Go to evaluations"),

        R("BackOffice.Home", "BackOffice.Home.Cards.Process.Tag", "Süreç", "Process"),
        R("BackOffice.Home", "BackOffice.Home.Cards.Process.Title", "İşlem Akışı", "Process Flow"),
        R("BackOffice.Home", "BackOffice.Home.Cards.Process.Description", "Bildiri gönderimi, revizyon, hakem değerlendirmesi ve sonuç kararları rolüne göre farklı işlem adımlarından oluşur.", "Submission, revision, reviewer evaluation and final decision processes consist of different steps depending on your role."),
        R("BackOffice.Home", "BackOffice.Home.Cards.Process.Link", "Akışı incele", "Review the flow"),

        R("BackOffice.Home", "BackOffice.Home.Cards.Congress.Tag", "Kongre", "Congress"),
        R("BackOffice.Home", "BackOffice.Home.Cards.Congress.Title", "Aktif Kongre", "Active Congress"),
        R("BackOffice.Home", "BackOffice.Home.Cards.Congress.Description", "Seçili kongre, başvuru ve değerlendirme ekranlarında çalışma kapsamını belirler.", "The selected congress determines the working scope for submission and evaluation screens."),
        R("BackOffice.Home", "BackOffice.Home.Cards.Congress.Link", "Kongre kapsamı sistem tarafından belirlenir", "Congress scope is determined by the system"),

        R("BackOffice.Home", "BackOffice.Home.Workflow.Title", "Genel kullanım akışı", "General usage flow"),
        R("BackOffice.Home", "BackOffice.Home.Workflow.Subtitle", "Yetkine göre kullanabileceğin temel işlem adımları", "Basic steps you can use according to your permissions"),

        R("BackOffice.Home", "BackOffice.Home.Workflow.Step1.Title", "Kongre bağlamını kontrol et", "Check congress context"),
        R("BackOffice.Home", "BackOffice.Home.Workflow.Step1.Description", "İşlem yaptığın kongre kapsamı sistemdeki aktif çalışma alanına göre belirlenir.", "The congress scope you are working in is determined by the active workspace in the system."),

        R("BackOffice.Home", "BackOffice.Home.Workflow.Step2.Title", "Sana açık menüleri kullan", "Use the menus available to you"),
        R("BackOffice.Home", "BackOffice.Home.Workflow.Step2.Description", "Yazar, hakem, editör veya yönetici rolüne göre erişebileceğin menüler otomatik olarak belirlenir.", "The menus you can access are determined automatically according to your author, reviewer, editor or administrator role."),

        R("BackOffice.Home", "BackOffice.Home.Workflow.Step3.Title", "Bekleyen işlemlerini takip et", "Track your pending actions"),
        R("BackOffice.Home", "BackOffice.Home.Workflow.Step3.Description", "Bildiri, revizyon, değerlendirme veya karar süreçlerinde senden beklenen adımları kontrol edebilirsin.", "You can check the steps expected from you in submission, revision, evaluation or decision processes."),

        R("BackOffice.Home", "BackOffice.Home.Workflow.Step4.Title", "Sonuç ve geçmişi incele", "Review results and history"),
        R("BackOffice.Home", "BackOffice.Home.Workflow.Step4.Description", "Tamamlanan işlemlerin durumunu, geçmişini ve varsa sonuç belgelerini ilgili ekranlardan görüntüleyebilirsin.", "You can view the status, history and result documents of completed actions from the related screens."),

        R("BackOffice.Home", "BackOffice.Home.Notes.Title", "Bilgilendirme", "Information"),
        R("BackOffice.Home", "BackOffice.Home.Notes.Authorization.Title", "Rol bazlı erişim", "Role-based access"),
        R("BackOffice.Home", "BackOffice.Home.Notes.Authorization.Description", "Bu sayfa tüm kullanıcılar için ortaktır. Menü ve işlem izinleri kullanıcı rolüne göre değişir.", "This page is shared by all users. Menu and action permissions vary according to the user role."),

        R("BackOffice.Home", "BackOffice.Home.Notes.Process.Title", "Senden beklenen işlemler", "Actions expected from you"),
        R("BackOffice.Home", "BackOffice.Home.Notes.Process.Description", "Revizyon, değerlendirme veya karar bekleyen işler ilgili menülerde görüntülenir.", "Revision, evaluation or decision-related pending actions are displayed in the related menus."),

        R("BackOffice.Home", "BackOffice.Home.Notes.Language.Title", "Dil desteği", "Language support"),
        R("BackOffice.Home", "BackOffice.Home.Notes.Language.Description", "Arayüz metinleri seçili dile göre veritabanındaki çeviri kaynaklarından okunur.", "Interface texts are read from database localization resources according to the selected language."),
    };
}
