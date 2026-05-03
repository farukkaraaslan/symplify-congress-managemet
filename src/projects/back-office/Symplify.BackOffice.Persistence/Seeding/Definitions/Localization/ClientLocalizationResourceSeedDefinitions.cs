namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class ClientLocalizationResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new("Common", "Common.Code", "Kod", "Code"),
        new("Common", "Common.SystemKey", "Sistem Anahtarı", "System Key"),
        new("Common", "Common.Select", "Seçiniz", "Select"),
        new("Common", "Common.SelectPlease", "Lütfen seçiniz", "Please select"),

        // WorkflowTemplate view compatibility aliases.
        // Current WebUI views use these keys directly; existing workflow seed also contains the Fields.* / *ModalTitle variants.
        new("BackOffice.WorkflowTemplates", "BackOffice.WorkflowTemplates.CreateTitle", "Workflow Şablonu Oluştur", "Create Workflow Template"),
        new("BackOffice.WorkflowTemplates", "BackOffice.WorkflowTemplates.UpdateTitle", "Workflow Şablonu Düzenle", "Edit Workflow Template"),
        new("BackOffice.WorkflowTemplates", "BackOffice.WorkflowTemplates.DeleteTitle", "Workflow Şablonunu Sil", "Delete Workflow Template"),
        new("BackOffice.WorkflowTemplates", "BackOffice.WorkflowTemplates.InitialTransactionStatus", "Başlangıç Durumu", "Initial Status"),
        new("BackOffice.WorkflowTemplates", "BackOffice.WorkflowTemplates.IsDefault", "Varsayılan", "Default"),
        new("BackOffice.WorkflowTemplates", "BackOffice.WorkflowTemplates.IsActive", "Aktif", "Active"),

        // WorkflowTemplateTransition view compatibility aliases.
        new("BackOffice.WorkflowTemplateTransitions", "BackOffice.WorkflowTemplateTransitions.CreateTitle", "Şablon Geçişi Ekle", "Add Template Transition"),
        new("BackOffice.WorkflowTemplateTransitions", "BackOffice.WorkflowTemplateTransitions.UpdateTitle", "Şablon Geçişi Düzenle", "Edit Template Transition"),
        new("BackOffice.WorkflowTemplateTransitions", "BackOffice.WorkflowTemplateTransitions.DeleteTitle", "Şablon Geçişini Sil", "Delete Template Transition"),
        new("BackOffice.WorkflowTemplateTransitions", "BackOffice.WorkflowTemplateTransitions.Transition", "Geçiş", "Transition"),
        new("BackOffice.WorkflowTemplateTransitions", "BackOffice.WorkflowTemplateTransitions.IsActive", "Aktif", "Active")
    };
}
