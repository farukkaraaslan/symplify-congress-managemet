namespace Symplify.BackOffice.Application.Services.Mailing;

public sealed class RenderedSystemMailTemplate
{
    public string Subject { get; set; } = string.Empty;

    public string HtmlBody { get; set; } = string.Empty;
}
