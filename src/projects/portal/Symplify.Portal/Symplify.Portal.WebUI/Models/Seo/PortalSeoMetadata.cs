namespace Symplify.Portal.WebUI.Models.Seo;

public sealed class PortalSeoMetadata
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Keywords { get; init; } = string.Empty;
    public string SiteName { get; init; } = string.Empty;
    public string CanonicalUrl { get; init; } = string.Empty;
    public string BrandColor { get; init; } = "#0f3b5f";
    public string OpenGraphLocale { get; init; } = "tr_TR";
    public string FaviconLightUrl { get; init; } = string.Empty;
    public string FaviconDarkUrl { get; init; } = string.Empty;
    public string AppleTouchIconUrl { get; init; } = string.Empty;
    public string SocialImageUrl { get; init; } = string.Empty;
    public IReadOnlyList<PortalAlternateLanguageLink> AlternateLanguages { get; init; } = Array.Empty<PortalAlternateLanguageLink>();
    public IReadOnlyList<string> JsonLdBlocks { get; init; } = Array.Empty<string>();
}

public sealed class PortalAlternateLanguageLink
{
    public string Hreflang { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}
