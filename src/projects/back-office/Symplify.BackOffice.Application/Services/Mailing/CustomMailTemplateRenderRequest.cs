namespace Symplify.BackOffice.Application.Services.Mailing;

public sealed class CustomMailTemplateRenderRequest
{
    public Guid? LanguageId { get; set; }

    public string? Culture { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Must already be produced by a trusted renderer. Raw user HTML must never be assigned here.
    /// </summary>
    public string SafeBodyHtml { get; set; } = string.Empty;

    public MailBrandingModel Branding { get; set; } = new();
}
