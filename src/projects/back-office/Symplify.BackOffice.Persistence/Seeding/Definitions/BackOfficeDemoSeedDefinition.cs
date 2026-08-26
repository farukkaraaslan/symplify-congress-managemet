using Symplify.BackOffice.Domain.Enums;

namespace Symplify.BackOffice.Persistence.Seeding.Definitions;

internal static class BackOfficeDemoSeedDefinition
{
    internal const string SystemActor = "SystemSeed";

    internal const string DefaultPassword = "Admin1234!";

    internal const string SuperAdminRoleName = "SuperAdmin";
    internal const string OrganizationAdminRoleName = "OrganizationAdmin";
    internal const string CongressEditorRoleName = "CongressEditor";
    internal const string ReviewerRoleName = "Reviewer";
    internal const string AuthorRoleName = "Author";

    internal static readonly Guid UtsakOrganizationId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    internal static readonly Guid UbakOrganizationId = Guid.Parse("10000000-0000-0000-0000-000000000002");

    internal static readonly Guid UtsakCongressId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    internal static readonly Guid UbakCongressId = Guid.Parse("20000000-0000-0000-0000-000000000002");

    internal static readonly Guid OrganizationId = UtsakOrganizationId;
    internal static readonly Guid CongressId = UtsakCongressId;
    internal static readonly Guid DefaultWorkflowTemplateId = Guid.Parse("4fcb16ae-8b54-4d32-a0d8-51031be46fc9");

    internal static IReadOnlyList<OrganizationSeed> Organizations => new[]
    {
        new OrganizationSeed(
            UtsakOrganizationId,
            "UTSAK",
            "UTSAK",
            "UTSAK",
            "utsak",
            "https://www.utsakcongress.com",
            "www.utsakcongress.com",
            "Uluslararası tıp ve sağlık bilimleri kongre organizasyonu.",
            "info@utsakcongress.com",
            "#0f3b5f"),

        new OrganizationSeed(
            UbakOrganizationId,
            "UBAK",
            "UBAK",
            "UBAK",
            "ubak",
            "https://ubaksymposium.org",
            "ubaksymposium.org",
            "Uluslararası bilimsel araştırmalar kongre organizasyonu.",
            "info@ubaksymposium.org",
            "#ef4444")
    };

    internal static IReadOnlyList<CongressSeed> Congresses => new[]
    {
        new CongressSeed(
            UtsakCongressId,
            UtsakOrganizationId,
            "UTSAK-2026-001",
            "22. Uluslararası Tıp ve Sağlık Bilimleri Araştırmaları Kongresi",
            "22-uluslararasi-tip-ve-saglik-bilimleri-arastirmalari-kongresi",
            22,
            new DateTime(2026, 7, 18, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Utc),
            "Ankara",
            "Ankara / Türkiye",
            "info@utsakcongress.com",
            "22. Uluslararası Tıp ve Sağlık Bilimleri Araştırmaları Kongresi",
            "22nd International Medical and Health Sciences Research Congress",
            "Tıp ve sağlık bilimleri alanında güncel akademik çalışmalar.",
            "Current academic studies in medical and health sciences.",
            "18 - 19 Temmuz 2026 tarihlerinde Ankara’da gerçekleştirilecek uluslararası kongre.",
            "An international congress to be held in Ankara on 18 - 19 July 2026.",
            "Değerli Araştırmacılar",
            "Dear Researchers",
            "Daha önce farklı tarihlerde gerçekleştirilen kongreler serisinin devamı olan 22. Uluslararası Tıp ve Sağlık Bilimleri Araştırmaları Kongresi, sizlerin destekleriyle 18 - 19 Temmuz 2026 tarihleri arasında gerçekleştirilecektir.",
            "The 22nd International Medical and Health Sciences Research Congress will be held on 18 - 19 July 2026 with your valuable contributions.",
            Guid.Parse("32000000-0000-0000-0000-000000000001")),

        new CongressSeed(
            UbakCongressId,
            UbakOrganizationId,
            "UBAK-2026-001",
            "26. Uluslararası Bilimsel Araştırmalar Kongresi",
            "26-uluslararasi-bilimsel-arastirmalar-kongresi",
            26,
            new DateTime(2026, 7, 11, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc),
            "Ankara",
            "Ankara / Türkiye",
            "info@ubaksymposium.org",
            "26. Uluslararası Bilimsel Araştırmalar Kongresi",
            "26th International Scientific Research Congress",
            "Sosyal, eğitim ve bilimsel araştırmalar alanında uluslararası kongre.",
            "International congress in social, educational and scientific research.",
            "11 - 12 Temmuz 2026 tarihlerinde Ankara’da gerçekleştirilecek uluslararası bilimsel araştırmalar kongresi.",
            "An international scientific research congress to be held in Ankara on 11 - 12 July 2026.",
            "Değerli Araştırmacılar",
            "Dear Researchers",
            "Daha önce farklı tarihlerde yapılan kongrelerin devamı olarak 26. Uluslararası Bilimsel Araştırmalar Kongresi, sizlerin destekleriyle 11 - 12 Temmuz 2026 tarihleri arasında yapılacaktır.",
            "The 26th International Scientific Research Congress will be held on 11 - 12 July 2026 with your valuable contributions.",
            Guid.Parse("32000000-0000-0000-0000-000000000002"))
    };

    internal static IReadOnlyList<TestUserSeed> TestUsers => new[]
    {
        new TestUserSeed(Guid.Parse("40000000-0000-0000-0000-000000000001"), UtsakOrganizationId, UtsakCongressId, OrganizationAdminRoleName, "admin@utsakcongress.com", "UTSAK", "Admin", "UTSAK", null),
        new TestUserSeed(Guid.Parse("40000000-0000-0000-0000-000000000002"), UtsakOrganizationId, UtsakCongressId, CongressEditorRoleName, "editor@utsakcongress.com", "UTSAK", "Editör", "UTSAK", null),
        new TestUserSeed(Guid.Parse("40000000-0000-0000-0000-000000000003"), UtsakOrganizationId, UtsakCongressId, ReviewerRoleName, "reviewer@utsakcongress.com", "UTSAK", "Hakem", "UTSAK", "0000-0001-0000-0001"),
        new TestUserSeed(Guid.Parse("40000000-0000-0000-0000-000000000004"), UtsakOrganizationId, UtsakCongressId, AuthorRoleName, "author@utsakcongress.com", "UTSAK", "Yazar", "UTSAK", "0000-0001-0000-0002"),

        new TestUserSeed(Guid.Parse("40000000-0000-0000-0000-000000000101"), UbakOrganizationId, UbakCongressId, OrganizationAdminRoleName, "admin@ubaksymposium.org", "UBAK", "Admin", "UBAK", null),
        new TestUserSeed(Guid.Parse("40000000-0000-0000-0000-000000000102"), UbakOrganizationId, UbakCongressId, CongressEditorRoleName, "editor@ubaksymposium.org", "UBAK", "Editör", "UBAK", null),
        new TestUserSeed(Guid.Parse("40000000-0000-0000-0000-000000000103"), UbakOrganizationId, UbakCongressId, ReviewerRoleName, "reviewer@ubaksymposium.org", "UBAK", "Hakem", "UBAK", "0000-0002-0000-0001"),
        new TestUserSeed(Guid.Parse("40000000-0000-0000-0000-000000000104"), UbakOrganizationId, UbakCongressId, AuthorRoleName, "author@ubaksymposium.org", "UBAK", "Yazar", "UBAK", "0000-0002-0000-0002")
    };

    internal static IReadOnlyList<LookupSeed> Titles => new[]
    {
        new LookupSeed(Guid.Parse("11000000-0000-0000-0000-000000000001"), "PROF_DR", 1, "Profesör Doktor", "Professor Doctor", "Prof. Dr.", "Prof. Dr."),
        new LookupSeed(Guid.Parse("11000000-0000-0000-0000-000000000002"), "ASSOC_PROF_DR", 2, "Doçent Doktor", "Associate Professor Doctor", "Doç. Dr.", "Assoc. Prof. Dr."),
        new LookupSeed(Guid.Parse("11000000-0000-0000-0000-000000000003"), "ASST_PROF_DR", 3, "Doktor Öğretim Üyesi", "Assistant Professor Doctor", "Dr. Öğr. Üyesi", "Asst. Prof. Dr."),
        new LookupSeed(Guid.Parse("11000000-0000-0000-0000-000000000004"), "DR", 4, "Doktor", "Doctor", "Dr.", "Dr."),
        new LookupSeed(Guid.Parse("11000000-0000-0000-0000-000000000005"), "SPEC", 5, "Uzman", "Specialist", null, null)
    };

    internal static IReadOnlyList<LookupSeed> DocumentTypes => new[]
    {
        new LookupSeed(Guid.Parse("12000000-0000-0000-0000-000000000001"), "ACCEPTANCE_LETTER", 1, "Kabul Yazısı", "Acceptance Letter", "Kabul edilen bildiriler için oluşturulan resmi yazı.", "Official letter generated for accepted submissions."),
        new LookupSeed(Guid.Parse("12000000-0000-0000-0000-000000000002"), "FULL_TEXT_TEMPLATE", 2, "Tam Metin Şablonu", "Full Text Template", null, null),
        new LookupSeed(Guid.Parse("12000000-0000-0000-0000-000000000003"), "PROGRAM", 3, "Kongre Programı", "Congress Program", null, null),
        new LookupSeed(Guid.Parse("12000000-0000-0000-0000-000000000004"), "CERTIFICATE", 4, "Katılım Belgesi", "Certificate", null, null)
    };

    internal static IReadOnlyList<LookupSeed> SubmissionTypes => new[]
    {
        new LookupSeed(Guid.Parse("13000000-0000-0000-0000-000000000001"), "ORAL", 1, "Sözlü Bildiri", "Oral Presentation", "Kongrede sözlü sunum olarak değerlendirilir.", "Evaluated as an oral presentation."),
        new LookupSeed(Guid.Parse("13000000-0000-0000-0000-000000000002"), "POSTER", 2, "Poster Bildiri", "Poster Presentation", "Kongrede poster sunumu olarak değerlendirilir.", "Evaluated as a poster presentation."),
        new LookupSeed(Guid.Parse("13000000-0000-0000-0000-000000000003"), "ONLINE_ORAL", 3, "Online Sözlü Bildiri", "Online Oral Presentation", null, null),
        new LookupSeed(Guid.Parse("13000000-0000-0000-0000-000000000004"), "CASE_REPORT", 4, "Olgu Sunumu", "Case Report", null, null),
        new LookupSeed(Guid.Parse("13000000-0000-0000-0000-000000000005"), "EXHIBITION", 5, "Sergi Başvurusu", "Exhibition Application", "Sergi/eser başvuruları için kullanılır.", "Used for exhibition/artwork applications.", SubmissionFormProfile.ExhibitionApplication)
    };

    internal static IReadOnlyList<LookupSeed> Topics => new[]
    {
        new LookupSeed(Guid.Parse("14000000-0000-0000-0000-000000000001"), "GENERAL_MEDICINE", 1, "Genel Tıp", "General Medicine", null, null),
        new LookupSeed(Guid.Parse("14000000-0000-0000-0000-000000000002"), "PUBLIC_HEALTH", 2, "Halk Sağlığı", "Public Health", null, null),
        new LookupSeed(Guid.Parse("14000000-0000-0000-0000-000000000003"), "NURSING", 3, "Hemşirelik", "Nursing", null, null),
        new LookupSeed(Guid.Parse("14000000-0000-0000-0000-000000000004"), "DENTISTRY", 4, "Diş Hekimliği", "Dentistry", null, null),
        new LookupSeed(Guid.Parse("14000000-0000-0000-0000-000000000005"), "PHYSIOTHERAPY", 5, "Fizyoterapi ve Rehabilitasyon", "Physiotherapy and Rehabilitation", null, null),
        new LookupSeed(Guid.Parse("14000000-0000-0000-0000-000000000006"), "NUTRITION", 6, "Beslenme ve Diyetetik", "Nutrition and Dietetics", null, null)
    };

    internal static IReadOnlyList<LookupSeed> EvaluationCriteria => new[]
    {
        new LookupSeed(Guid.Parse("15000000-0000-0000-0000-000000000001"), "SCIENTIFIC_QUALITY", 1, "Bilimsel Nitelik", "Scientific Quality", null, null),
        new LookupSeed(Guid.Parse("15000000-0000-0000-0000-000000000002"), "ORIGINALITY", 2, "Özgünlük", "Originality", null, null),
        new LookupSeed(Guid.Parse("15000000-0000-0000-0000-000000000003"), "METHODOLOGY", 3, "Yöntem", "Methodology", null, null),
        new LookupSeed(Guid.Parse("15000000-0000-0000-0000-000000000004"), "PRESENTATION", 4, "Sunum ve Anlatım", "Presentation", null, null),
        new LookupSeed(Guid.Parse("15000000-0000-0000-0000-000000000005"), "RELEVANCE", 5, "Kongre Konularına Uygunluk", "Relevance to Congress Topics", null, null)
    };

    internal static IReadOnlyList<LookupSeed> EventRooms => new[]
    {
        new LookupSeed(Guid.Parse("16000000-0000-0000-0000-000000000001"), "MAIN_HALL", 1, "Ana Salon", "Main Hall", null, null),
        new LookupSeed(Guid.Parse("16000000-0000-0000-0000-000000000002"), "HALL_A", 2, "Salon A", "Hall A", null, null),
        new LookupSeed(Guid.Parse("16000000-0000-0000-0000-000000000003"), "ONLINE_ROOM", 3, "Online Salon", "Online Room", null, null)
    };

    internal static IReadOnlyList<TransactionStatusPhaseSeed> WorkflowPhases => new[]
    {
        new TransactionStatusPhaseSeed(10, "SUBMISSION", 1, "Başvuru", "Submission", "Bildiri gönderim ve ön kontrol süreci.", "Submission and pre-check stage."),
        new TransactionStatusPhaseSeed(20, "REVIEW", 2, "Hakem Değerlendirme", "Review", "Hakem atama ve değerlendirme süreci.", "Reviewer assignment and evaluation stage."),
        new TransactionStatusPhaseSeed(30, "DECISION", 3, "Editör Kararı", "Editorial Decision", "Editör/komite karar süreci.", "Editorial or committee decision stage."),
        new TransactionStatusPhaseSeed(40, "POST_ACCEPTANCE", 4, "Kabul Sonrası", "Post Acceptance", "Kabul sonrası belge, ödeme ve tamamlama süreci.", "Post-acceptance documents, payment and completion stage."),
        new TransactionStatusPhaseSeed(50, "FINAL", 5, "Kapanış", "Final", "Sürecin kapandığı nihai durumlar.", "Final closed statuses.")
    };

    internal static IReadOnlyList<TransactionStatusSeed> WorkflowStatuses => new[]
    {
        new TransactionStatusSeed(100, "DRAFT", 10, 1, true, false, "Taslak", "Draft", "Yazar tarafından düzenlenebilir taslak bildiri.", "Editable draft submission."),
        new TransactionStatusSeed(110, "SUBMITTED", 10, 2, false, false, "Gönderildi", "Submitted", "Yazar tarafından onaya gönderildi.", "Submitted for processing."),
        new TransactionStatusSeed(120, "PRE_CHECK", 10, 3, false, false, "Ön Kontrol", "Pre-check", "Sekretarya veya editör ön kontrolünde.", "Under secretariat or editorial pre-check."),
        new TransactionStatusSeed(130, "REVIEWER_ASSIGNMENT", 20, 1, false, false, "Hakem Ataması", "Reviewer Assignment", "Hakem ataması bekleniyor.", "Waiting for reviewer assignment."),
        new TransactionStatusSeed(140, "UNDER_REVIEW", 20, 2, false, false, "Hakem Değerlendirmesinde", "Under Review", "Hakem değerlendirmesi devam ediyor.", "Reviewer evaluation is in progress."),
        new TransactionStatusSeed(150, "REVIEWS_COMPLETED", 20, 3, false, false, "Değerlendirmeler Tamamlandı", "Reviews Completed", "Hakem değerlendirmeleri tamamlandı.", "Reviewer evaluations are completed."),
        new TransactionStatusSeed(160, "EDITORIAL_DECISION", 30, 1, false, false, "Editör Kararı", "Editorial Decision", "Komite/editör kararı bekleniyor.", "Waiting for editorial decision."),
        new TransactionStatusSeed(170, "REVISION_REQUESTED", 30, 2, true, false, "Revizyon İstendi", "Revision Requested", "Yazardan revizyon bekleniyor.", "Waiting for author revision."),
        new TransactionStatusSeed(180, "ACCEPTED", 30, 3, false, false, "Kabul Edildi", "Accepted", "Bildiri kabul edildi.", "Submission has been accepted."),
        new TransactionStatusSeed(190, "REJECTED", 50, 1, false, true, "Reddedildi", "Rejected", "Bildiri reddedildi.", "Submission has been rejected."),
        new TransactionStatusSeed(200, "PAYMENT_PENDING", 40, 1, false, false, "Ödeme Bekliyor", "Payment Pending", "Kabul sonrası ödeme durumu bilgisidir; bildiri karar akışında kullanılmaz.", "Post-acceptance payment status marker; not used as a submission decision status."),
        new TransactionStatusSeed(210, "COMPLETED", 50, 2, false, true, "Tamamlandı", "Completed", "Bildiri süreci tamamlandı.", "Submission process has been completed."),
        new TransactionStatusSeed(220, "WITHDRAWN", 50, 3, false, true, "Geri Çekildi", "Withdrawn", "Yazar veya yönetim tarafından geri çekildi.", "Withdrawn by author or administration.")
    };

    internal static IReadOnlyList<TransactionStatusTransitionSeed> WorkflowTransitions => new[]
    {
        new TransactionStatusTransitionSeed(1000, "DRAFT", "SUBMITTED", 1, false, "Onaya Gönder", "Submit", "Bildiri onaya gönderilir.", "Submit the draft for processing."),
        new TransactionStatusTransitionSeed(1015, "SUBMITTED", "REVIEWER_ASSIGNMENT", 2, false, "Hakeme Gönder", "Send to Review", "Bildiri hakem atama sürecine alınır.", "Move the submission to reviewer assignment."),
        new TransactionStatusTransitionSeed(1025, "SUBMITTED", "REVISION_REQUESTED", 3, false, "Revizyon İste", "Request Revision", "Editör ilk kontrolde yazardan revizyon ister.", "Request revision from the author during initial editorial check."),
        new TransactionStatusTransitionSeed(1035, "SUBMITTED", "REJECTED", 4, false, "Reddet", "Reject", "Editör ilk kontrolde bildiriyi reddeder.", "Reject the submission during initial editorial check."),
        new TransactionStatusTransitionSeed(1045, "REVIEWER_ASSIGNMENT", "UNDER_REVIEW", 5, true, "Değerlendirme Başladı", "Review Started", "Hakem ataması sonrası değerlendirme başlar.", "The evaluation starts after reviewer assignment."),
        new TransactionStatusTransitionSeed(1055, "UNDER_REVIEW", "EDITORIAL_DECISION", 6, true, "Karar Aşamasına Al", "Move to Decision", "Hakem değerlendirmeleri tamamlandıktan sonra karar aşamasına geçilir.", "Move to decision after reviewer evaluations are completed."),
        new TransactionStatusTransitionSeed(1060, "EDITORIAL_DECISION", "ACCEPTED", 7, false, "Kabul Et", "Accept", "Bildiri kabul edilir ve kabul işlemleri başlatılır.", "Accept the submission and start post-acceptance actions."),
        new TransactionStatusTransitionSeed(1070, "EDITORIAL_DECISION", "REJECTED", 8, false, "Reddet", "Reject", null, null),
        new TransactionStatusTransitionSeed(1080, "EDITORIAL_DECISION", "REVISION_REQUESTED", 9, false, "Revizyon İste", "Request Revision", null, null),
        new TransactionStatusTransitionSeed(1090, "REVISION_REQUESTED", "SUBMITTED", 10, false, "Revizyonu Tekrar Gönder", "Resubmit Revision", null, null),
        new TransactionStatusTransitionSeed(1120, "DRAFT", "WITHDRAWN", 13, false, "Geri Çek", "Withdraw", null, null),
        new TransactionStatusTransitionSeed(1130, "SUBMITTED", "WITHDRAWN", 14, false, "Geri Çek", "Withdraw", null, null)
    };

    internal static IReadOnlyList<WorkflowEffectSeed> WorkflowEffects => new[]
    {
        new WorkflowEffectSeed(Guid.Parse("4b19bf06-81a7-4533-9c2d-8e7a2926d0d6"), "SUBMITTED", "REVIEWER_ASSIGNMENT", WorkflowEffectType.QueueSubmissionStatusEmail, 1, "{\"templateCode\":\"SUBMISSION_SENT_TO_REVIEW\"}"),
        new WorkflowEffectSeed(Guid.Parse("19ea97a7-f6ec-48c4-8d7f-1d8d0f511d40"), "EDITORIAL_DECISION", "ACCEPTED", WorkflowEffectType.GenerateAcceptanceLetter, 1, "{\"templateCode\":\"ACCEPTANCE_LETTER_DEFAULT\"}"),
        new WorkflowEffectSeed(Guid.Parse("8d82ee1d-7931-45f3-9549-64e4d3349b18"), "EDITORIAL_DECISION", "ACCEPTED", WorkflowEffectType.QueueAcceptanceEmail, 2, "{\"templateCode\":\"ACCEPTANCE_EMAIL_DEFAULT\",\"attachAcceptanceLetter\":true}")
    };

    internal sealed record OrganizationSeed(
        Guid Id,
        string Code,
        string ShortName,
        string Name,
        string Slug,
        string WebsiteUrl,
        string HostUrl,
        string Description,
        string ContactEmail,
        string BrandColor);

    internal sealed record CongressSeed(
        Guid Id,
        Guid OrganizationId,
        string Code,
        string Name,
        string Slug,
        int EditionNumber,
        DateTime StartDate,
        DateTime EndDate,
        string City,
        string VenueName,
        string ContactEmail,
        string TurkishTitle,
        string EnglishTitle,
        string TurkishSubtitle,
        string EnglishSubtitle,
        string TurkishShortDescription,
        string EnglishShortDescription,
        string TurkishWelcomeTitle,
        string EnglishWelcomeTitle,
        string TurkishWelcomeContent,
        string EnglishWelcomeContent,
        Guid WorkflowSettingId);

    internal sealed record TestUserSeed(
        Guid Id,
        Guid OrganizationId,
        Guid DefaultCongressId,
        string RoleName,
        string Email,
        string Name,
        string Surname,
        string Institution,
        string? Orcid);

    internal sealed record LookupSeed(Guid Id, string Code, int Order, string TurkishName, string EnglishName, string? TurkishDescription, string? EnglishDescription, SubmissionFormProfile FormProfile = SubmissionFormProfile.AcademicAbstract);
    internal sealed record TransactionStatusPhaseSeed(int Id, string Code, int Order, string TurkishName, string EnglishName, string? TurkishDescription, string? EnglishDescription);
    internal sealed record TransactionStatusSeed(int Id, string Code, int PhaseId, int Order, bool IsEditable, bool IsFinal, string TurkishName, string EnglishName, string? TurkishDescription, string? EnglishDescription);
    internal sealed record TransactionStatusTransitionSeed(
        int Id,
        string FromStatusCode,
        string ToStatusCode,
        int Order,
        bool IsAuto,
        string TurkishName,
        string EnglishName,
        string? TurkishDescription,
        string? EnglishDescription);
    internal sealed record WorkflowEffectSeed(Guid Id, string FromStatusCode, string ToStatusCode, WorkflowEffectType EffectType, int Order, string ParametersJson);
}
