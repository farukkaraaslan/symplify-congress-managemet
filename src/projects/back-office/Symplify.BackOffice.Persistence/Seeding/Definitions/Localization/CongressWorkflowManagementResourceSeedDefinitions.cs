namespace Symplify.BackOffice.Persistence.Seeding.Definitions.Localization;

public static class CongressWorkflowManagementResourceSeedDefinitions
{
    public static IReadOnlyCollection<ResourceSeedDefinition> All { get; } = new List<ResourceSeedDefinition>
    {
        new("Common", "Common.Refresh", "Yenile", "Refresh"),
        new("Common", "Common.Loading", "Yükleniyor...", "Loading..."),

        new("BackOffice.CongressWorkflows", "BackOffice.CongressWorkflows.Help", "Workflow şablonları sistem genelinde tanımlanır; kongreye uygulandığında geçişler kongreye özel olarak kopyalanır. Submission akışı bu kongreye bağlı workflow ayarını kullanır.", "Workflow templates are defined globally; when applied to a congress, transitions are copied into congress-specific workflow settings. The submission flow uses the workflow setting linked to this congress."),
        new("BackOffice.CongressWorkflows", "BackOffice.CongressWorkflows.ApplyTemplateTitle", "Workflow Şablonu Uygula", "Apply Workflow Template"),
        new("BackOffice.CongressWorkflows", "BackOffice.CongressWorkflows.CurrentWorkflowTitle", "Geçerli Workflow", "Current Workflow"),

        new("BackOffice.CongressWorkflows", "BackOffice.CongressWorkflows.Buttons.ApplyTemplate", "Şablonu Uygula", "Apply Template"),

        new("BackOffice.CongressWorkflows", "BackOffice.CongressWorkflows.Fields.ReplaceExistingTransitions", "Mevcut geçişleri şablonla değiştir", "Replace existing transitions with template"),
        new("BackOffice.CongressWorkflows", "BackOffice.CongressWorkflows.Fields.TransitionCount", "Geçiş Sayısı", "Transition Count"),
        new("BackOffice.CongressWorkflows", "BackOffice.CongressWorkflows.Fields.FromStatus", "Kaynak Durum", "From Status"),
        new("BackOffice.CongressWorkflows", "BackOffice.CongressWorkflows.Fields.ToStatus", "Hedef Durum", "To Status"),

        new("BackOffice.CongressWorkflows", "BackOffice.CongressWorkflows.Validation.TemplateRequired", "Workflow şablonu seçimi zorunludur.", "Workflow template selection is required."),

        new("BackOffice.CongressWorkflows", "BackOffice.CongressWorkflows.Messages.NoTemplates", "Aktif workflow şablonu bulunamadı. Önce Workflow Şablonları ekranından şablon oluşturun.", "No active workflow template was found. Create a template from the Workflow Templates screen first."),
        new("BackOffice.CongressWorkflows", "BackOffice.CongressWorkflows.Messages.NoWorkflow", "Bu kongreye henüz workflow şablonu uygulanmamış.", "No workflow template has been applied to this congress yet."),
        new("BackOffice.CongressWorkflows", "BackOffice.CongressWorkflows.Messages.NoTransitions", "Bu kongre için workflow geçişi bulunamadı.", "No workflow transitions were found for this congress."),
        new("BackOffice.CongressWorkflows", "BackOffice.CongressWorkflows.Messages.ApplyConfirmTitle", "Workflow şablonu uygulansın mı?", "Apply workflow template?"),
        new("BackOffice.CongressWorkflows", "BackOffice.CongressWorkflows.Messages.ApplyConfirmText", "Mevcut geçişler seçilen şablona göre güncellenecek.", "Existing transitions will be updated according to the selected template."),
    };
}
