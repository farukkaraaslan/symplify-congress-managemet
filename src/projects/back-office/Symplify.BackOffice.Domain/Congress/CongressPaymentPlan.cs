using Core.Persistence.Repositories;

namespace Symplify.BackOffice.Domain.Congress;

public class CongressPaymentPlan : Entity<Guid>, IEntityTimestamps, IAuditable
{
    public Guid CongressId { get; set; }

    /// <summary>
    /// Kongre içinde ödeme planını teknik olarak ayırt eden kod.
    /// Örn: DOMESTIC_ORAL_POSTER, INTERNATIONAL_PARTICIPATION.
    /// </summary>
    public string Code { get; set; } = null!;

    public decimal Amount { get; set; }

    /// <summary>
    /// ISO para birimi kodu. Örn: TRY, USD, EUR.
    /// </summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>
    /// All, Domestic, International.
    /// Kullanıcı/submission country bilgisine göre gösterilecek planları ayırmak için kullanılır.
    /// </summary>
    public string AudienceType { get; set; } = "All";

    /// <summary>
    /// Participation, SecondSubmission, Listener, Student, Other vb.
    /// Raporlama ve admin filtreleri için kullanılır.
    /// </summary>
    public string PaymentCategory { get; set; } = "Participation";

    /// <summary>
    /// Geriye uyumluluk için korunur. Yeni ekranda ValidUntil ile aynı amaçta kullanılabilir.
    /// </summary>
    public DateTime? DueDate { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public int Order { get; set; }

    public bool IsPublicVisible { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public virtual Congress Congress { get; set; } = null!;

    public virtual ICollection<CongressPaymentPlanTranslation> Translations { get; set; } = new HashSet<CongressPaymentPlanTranslation>();
}
