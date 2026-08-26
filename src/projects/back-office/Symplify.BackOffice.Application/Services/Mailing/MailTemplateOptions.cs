namespace Symplify.BackOffice.Application.Services.Mailing;

public sealed class MailTemplateOptions
{
    public const string SectionName = "Mail";

    public string BrandName { get; set; } = "Symplify";

    public string FallbackLogoPath { get; set; } = "/assets/images/logo/symplify-logo-horizontal-light.svg";
}
