using Core.Persistence.Repositories;
using Symplify.BackOffice.Domain.Common;
using Symplify.BackOffice.Domain.Enums;
using OrganizationEntity = Symplify.BackOffice.Domain.Organization.Organization;
using Symplify.BackOffice.Domain.Reference;

namespace Symplify.BackOffice.Domain.Congress;

public class Congress : Entity<Guid>, IEntityTimestamps, IAuditable, IAggregateRoot
{
    public Guid OrganizationId { get; set; }

    // Sistem tarafından üretilecek teknik kod. Kullanıcıdan alınmamalı.
    // Önerilen format: {OrganizationCode}-{Year}-{Sequence}, örn: UTSAK-2026-001
    public string Code { get; set; } = null!;

    // Mevcut kod ve listeleme uyumluluğu için tutulur. Default dil başlığı ile senkronlanabilir.
    public string Name { get; set; } = null!;

    // Create sırasında sistem üretir. Gerekirse edit ekranında gelişmiş ayar olarak düzenlenebilir.
    public string? Slug { get; set; }

    // 22. Kongre gibi dönem/sıra bilgisi teknik Code içine gömülmesin diye ayrı tutulur.
    public int? EditionNumber { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public CongressStatus Status { get; set; } = CongressStatus.Draft;

    // Kongreye özel iletişim bilgileri. Organization iletişiminden farklı olabilir.
    public string? ContactName { get; set; }

    public string? ContactTitle { get; set; }

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public string? ContactAddress { get; set; }

    // Kongre lokasyon bilgileri.
    public string? VenueName { get; set; }

    // Create sırasında bağlı organizasyonun tema logolarından kopyalanır.
    // Edit ekranında kongreye özel light/dark logo override edilebilir.
    public string? LogoLightPath { get; set; }

    public string? LogoDarkPath { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? CityId { get; set; }

    public Guid? StateId { get; set; }

    public virtual OrganizationEntity Organization { get; set; } = null!;

    public virtual Country? Country { get; set; }

    public virtual City? City { get; set; }

    public virtual State? State { get; set; }

    public virtual CongressWorkflowSetting? WorkflowSetting { get; set; }

    public virtual ICollection<CongressTranslation> Translations { get; set; } = new HashSet<CongressTranslation>();

    public virtual ICollection<CongressContactEmail> ContactEmails { get; set; } = new HashSet<CongressContactEmail>();

    public virtual ICollection<CongressSection> Sections { get; set; } = new HashSet<CongressSection>();

    public virtual ICollection<CongressSlider> Sliders { get; set; } = new HashSet<CongressSlider>();

    public virtual ICollection<CongressBoard> Boards { get; set; } = new HashSet<CongressBoard>();

    public virtual ICollection<CongressImportantDate> ImportantDates { get; set; } = new HashSet<CongressImportantDate>();

    public virtual ICollection<CongressPaymentPlan> PaymentPlans { get; set; } = new HashSet<CongressPaymentPlan>();

    public virtual ICollection<CongressDocument> Documents { get; set; } = new HashSet<CongressDocument>();

    public virtual ICollection<CongressAnnouncement> Announcements { get; set; } = new HashSet<CongressAnnouncement>();

    public virtual ICollection<CongressTopic> Topics { get; set; } = new HashSet<CongressTopic>();

    public virtual ICollection<CongressTopicCategory> TopicCategories { get; set; } = new HashSet<CongressTopicCategory>();

    public virtual ICollection<CongressSubmissionType> SubmissionTypes { get; set; } = new HashSet<CongressSubmissionType>();

    public virtual ICollection<CongressEvaluationCriterion> EvaluationCriteria { get; set; } = new HashSet<CongressEvaluationCriterion>();

    public virtual ICollection<CongressTransactionStatusTransition> TransactionStatusTransitions { get; set; } =
        new HashSet<CongressTransactionStatusTransition>();
}
