using MediatR;
using Microsoft.AspNetCore.Mvc;
using Symplify.Api.Application.Features.PublicSite.Contexts;
using Symplify.Api.Application.Features.PublicSite.Queries;

namespace Symplify.Api.WebAPI.Controllers;

[ApiController]
[Route("api/v1/public-site")]
public sealed class PublicSiteController : ControllerBase
{
    private static readonly string[] ExplicitCultureHeaderNames =
    [
        "X-Culture",
        "X-Symplify-Culture"
    ];

    private readonly IMediator _mediator;
    private readonly IPublicApiContextAccessor _publicApiContextAccessor;

    public PublicSiteController(IMediator mediator, IPublicApiContextAccessor publicApiContextAccessor)
    {
        _mediator = mediator;
        _publicApiContextAccessor = publicApiContextAccessor;
    }

    [HttpGet("bootstrap")]
    public async Task<IActionResult> GetBootstrap([FromQuery] string? culture, CancellationToken cancellationToken)
    {
        PublicApiContext publicApiContext = GetPublicApiContext();
        string? resolvedCulture = ResolveCulture(culture);
        var response = await _mediator.Send(new GetPublicSiteBootstrapQuery(publicApiContext.OrganizationId, resolvedCulture), cancellationToken);
        return Ok(response);
    }

    [HttpGet("home")]
    public async Task<IActionResult> GetHome([FromQuery] string? culture, CancellationToken cancellationToken)
    {
        PublicApiContext publicApiContext = GetPublicApiContext();
        string? resolvedCulture = ResolveCulture(culture);
        var response = await _mediator.Send(new GetPublicSiteHomeQuery(publicApiContext.OrganizationId, resolvedCulture), cancellationToken);
        return Ok(response);
    }

    [HttpGet("boards")]
    public async Task<IActionResult> GetBoards([FromQuery] string? culture, CancellationToken cancellationToken)
    {
        PublicApiContext publicApiContext = GetPublicApiContext();
        string? resolvedCulture = ResolveCulture(culture);
        var response = await _mediator.Send(new GetPublicSiteBoardsQuery(publicApiContext.OrganizationId, resolvedCulture), cancellationToken);
        return Ok(response);
    }

    [HttpGet("sections")]
    public async Task<IActionResult> GetSections([FromQuery] string? culture, CancellationToken cancellationToken)
    {
        PublicApiContext publicApiContext = GetPublicApiContext();
        string? resolvedCulture = ResolveCulture(culture);
        var response = await _mediator.Send(new GetPublicSiteSectionsQuery(publicApiContext.OrganizationId, resolvedCulture), cancellationToken);
        return Ok(response);
    }

    [HttpGet("sections/{bindingKey}")]
    public async Task<IActionResult> GetSectionByBindingKey(
        [FromRoute] string bindingKey,
        [FromQuery] string? culture,
        CancellationToken cancellationToken)
    {
        PublicApiContext publicApiContext = GetPublicApiContext();
        string? resolvedCulture = ResolveCulture(culture);
        var response = await _mediator.Send(
            new GetPublicSiteSectionByBindingKeyQuery(publicApiContext.OrganizationId, bindingKey, resolvedCulture),
            cancellationToken);

        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments([FromQuery] string? culture, CancellationToken cancellationToken)
    {
        PublicApiContext publicApiContext = GetPublicApiContext();
        string? resolvedCulture = ResolveCulture(culture);
        var response = await _mediator.Send(new GetPublicSiteDocumentsQuery(publicApiContext.OrganizationId, resolvedCulture), cancellationToken);
        return Ok(response);
    }

    [HttpGet("contact")]
    public async Task<IActionResult> GetContact([FromQuery] string? culture, CancellationToken cancellationToken)
    {
        PublicApiContext publicApiContext = GetPublicApiContext();
        string? resolvedCulture = ResolveCulture(culture);
        var response = await _mediator.Send(new GetPublicSiteContactQuery(publicApiContext.OrganizationId, resolvedCulture), cancellationToken);
        return Ok(response);
    }

    [HttpGet("contents")]
    public async Task<IActionResult> GetContents([FromQuery] string? culture, CancellationToken cancellationToken)
    {
        PublicApiContext publicApiContext = GetPublicApiContext();
        string? resolvedCulture = ResolveCulture(culture);
        var response = await _mediator.Send(new GetPublicSiteContentsQuery(publicApiContext.OrganizationId, resolvedCulture), cancellationToken);
        return Ok(response);
    }

    private string? ResolveCulture(string? queryCulture)
    {
        // Portal resolve ettiği kültürü API'ye X-Culture ile gönderir. Bu yüzden API'de en güçlü kaynak budur.
        string? cultureFromHeader = ResolveFromExplicitCultureHeaders();
        if (!string.IsNullOrWhiteSpace(cultureFromHeader))
            return cultureFromHeader;

        // Direct API testleri ve route tabanlı kullanım için explicit route/query header'dan sonra gelir.
        string? cultureFromRoute = RouteData.Values["culture"]?.ToString();
        if (!string.IsNullOrWhiteSpace(cultureFromRoute))
            return cultureFromRoute;

        if (!string.IsNullOrWhiteSpace(queryCulture))
            return queryCulture;

        // Sadece direct API çağrılarında hiçbir explicit seçim yoksa Accept-Language fallback olur.
        return ResolveFromAcceptLanguageHeader();
    }

    private string? ResolveFromExplicitCultureHeaders()
    {
        foreach (string headerName in ExplicitCultureHeaderNames)
        {
            string? headerValue = Request.Headers[headerName].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerValue))
                return headerValue;
        }

        return null;
    }

    private string? ResolveFromAcceptLanguageHeader()
    {
        string? acceptLanguage = Request.Headers.AcceptLanguage.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(acceptLanguage))
            return null;

        string firstLanguage = acceptLanguage
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;

        return firstLanguage
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
    }

    private PublicApiContext GetPublicApiContext()
    {
        return _publicApiContextAccessor.Current
               ?? throw new InvalidOperationException("Public API context could not be resolved. Make sure API key middleware is registered before controllers.");
    }
}
