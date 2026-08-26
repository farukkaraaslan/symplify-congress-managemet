namespace Symplify.Api.Application.Features.PublicSite.Contexts;

public sealed class PublicApiContext
{
    public const string HttpContextItemKey = "Symplify.PublicApi.Context";

    public Guid OrganizationId { get; init; }
    public Guid ApiKeyId { get; init; }
    public string OrganizationCode { get; init; } = string.Empty;
    public string OrganizationName { get; init; } = string.Empty;
    public string KeyPrefix { get; init; } = string.Empty;
    public string PublicHost { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Scopes { get; init; } = Array.Empty<string>();
}
