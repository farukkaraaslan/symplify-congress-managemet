namespace Symplify.Portal.WebUI.Options;

public sealed class PublicApiOptions
{
    public const string SectionName = "PublicApi";

    public string BaseUrl { get; set; } = "http://localhost:5200";
    public string ApiKey { get; set; } = string.Empty;
    public string? PublicHost { get; set; }
    public string DefaultCulture { get; set; } = "tr-TR";
    public int TimeoutSeconds { get; set; } = 30;
    public int ShellCacheSeconds { get; set; } = 120;
}
