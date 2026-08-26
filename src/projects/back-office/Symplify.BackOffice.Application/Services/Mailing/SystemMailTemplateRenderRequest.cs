namespace Symplify.BackOffice.Application.Services.Mailing;

public sealed class SystemMailTemplateRenderRequest
{
    public Guid? LanguageId { get; set; }

    public string? Culture { get; set; }

    public string SubjectKey { get; set; } = string.Empty;

    public string TitleKey { get; set; } = string.Empty;

    public string BodyKey { get; set; } = string.Empty;

    public string? ActionTextKey { get; set; }

    public string? ActionUrl { get; set; }

    public MailBrandingModel Branding { get; set; } = new();

    public IDictionary<string, string?> Tokens { get; set; } = new Dictionary<string, string?>();

    public IList<MailInfoRowModel> InfoRows { get; set; } = new List<MailInfoRowModel>();

    public bool ShowIfNotRequestedMessage { get; set; } = true;
}
