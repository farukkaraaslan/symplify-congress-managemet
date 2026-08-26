namespace Symplify.BackOffice.WebUI.Models.CongressPaymentPlans;

public sealed class CreateCongressPaymentPlanViewModel
{
    public Guid CongressId { get; set; }
    public string? Code { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string AudienceType { get; set; } = "All";
    public string PaymentCategory { get; set; } = "Participation";
    public string? DueDateText { get; set; }
    public string? ValidFromText { get; set; }
    public string? ValidUntilText { get; set; }
    public bool IsPublicVisible { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public List<CongressPaymentPlanTranslationViewModel> Translations { get; set; } = new();
    public List<CongressPaymentPlanSelectOptionViewModel> AudienceTypeOptions { get; set; } = new();
    public List<CongressPaymentPlanSelectOptionViewModel> PaymentCategoryOptions { get; set; } = new();
    public List<CongressPaymentPlanSelectOptionViewModel> CurrencyOptions { get; set; } = new();
}
