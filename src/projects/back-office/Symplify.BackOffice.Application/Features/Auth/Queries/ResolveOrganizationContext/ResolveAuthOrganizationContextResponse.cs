namespace Symplify.BackOffice.Application.Features.Auth.Queries.ResolveOrganizationContext;

public sealed class ResolveAuthOrganizationContextResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ShortName { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? LogoLightPath { get; set; }

    public string? LogoDarkPath { get; set; }
}
