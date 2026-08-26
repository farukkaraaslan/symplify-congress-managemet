using Core.Application.Storage;
using Core.CrossCuttingConcerns.Exceptions.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Symplify.BackOffice.Application.Features.Auth.Commands.ConfirmEmail;
using Symplify.BackOffice.Application.Features.Auth.Commands.ForgotPassword;
using Symplify.BackOffice.Application.Features.Auth.Commands.Login;
using Symplify.BackOffice.Application.Features.Auth.Commands.Register;
using Symplify.BackOffice.Application.Features.Auth.Commands.ResetPassword;
using Symplify.BackOffice.Application.Features.Auth.Queries.GetRegisterOptions;
using Symplify.BackOffice.Application.Features.Auth.Queries.GetStatesByCountry;
using Symplify.BackOffice.Application.Features.Auth.Queries.ResolveOrganizationContext;
using Symplify.BackOffice.Application.Services.Email;
using Symplify.BackOffice.Application.Services.Mailing;
using Symplify.BackOffice.Application.Services.Urls;
using Symplify.BackOffice.WebUI.Localization;
using Symplify.BackOffice.WebUI.Models.Auth;
using Symplify.BackOffice.WebUI.Services.Auth;
using Symplify.BackOffice.WebUI.Services.Authentication;

namespace Symplify.BackOffice.WebUI.Controllers;

[Route("{culture?}/auth")]
public sealed class AuthController : Controller
{
    private readonly IMediator _mediator;
    private readonly IBackOfficeCookieSignInService _cookieSignInService;
    private readonly IApplicationMailQueueService _mailQueueService;
    private readonly ISystemMailTemplateRenderer _mailTemplateRenderer;
    private readonly IMailBrandingResolver _mailBrandingResolver;
    private readonly IBackOfficeViewLocalizer _localizer;
    private readonly ObjectStorageOptions _objectStorageOptions;
    private readonly IPublicUrlService _publicUrlService;

    public AuthController(
        IMediator mediator,
        IBackOfficeCookieSignInService cookieSignInService,
        IApplicationMailQueueService mailQueueService,
        ISystemMailTemplateRenderer mailTemplateRenderer,
        IMailBrandingResolver mailBrandingResolver,
        IBackOfficeViewLocalizer localizer,
        IOptions<ObjectStorageOptions> objectStorageOptions,
        IPublicUrlService publicUrlService)
    {
        _mediator = mediator;
        _cookieSignInService = cookieSignInService;
        _mailQueueService = mailQueueService;
        _mailTemplateRenderer = mailTemplateRenderer;
        _mailBrandingResolver = mailBrandingResolver;
        _localizer = localizer;
        _objectStorageOptions = objectStorageOptions.Value;
        _publicUrlService = publicUrlService;
    }

    [AllowAnonymous]
    [HttpGet("login")]
    public async Task<IActionResult> Login(
        string? returnUrl = null,
        string? org = null,
        CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        ResolveAuthOrganizationContextResponse? organizationContext = await ResolveAuthOrganizationContextAsync(org, cancellationToken);
        if (organizationContext is null)
            return OrganizationContextRequired();

        LoginViewModel model = new()
        {
            ReturnUrl = returnUrl
        };

        ApplyOrganizationContext(model, organizationContext);

        return View(model);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        CancellationToken cancellationToken)
    {
        ResolveAuthOrganizationContextResponse? organizationContext = await ResolveAuthOrganizationContextAsync(
            model.OrganizationSlug ?? model.OrganizationId?.ToString("D"),
            cancellationToken);
        if (organizationContext is null)
            return OrganizationContextRequired();

        ApplyOrganizationContext(model, organizationContext);

        if (!AuthModelStateLocalizer.ValidateLogin(model, ModelState, _localizer))
            return View(model);

        try
        {
            LoggedInResponse response = await _mediator.Send(
                new LoginCommand
                {
                    Email = model.Email,
                    Password = model.Password,
                    OrganizationId = organizationContext?.Id
                },
                cancellationToken);

            await _cookieSignInService.SignInAsync(
                HttpContext,
                response.User,
                model.RememberMe,
                cancellationToken);

            return RedirectToLocal(model.ReturnUrl);
        }
        catch (BusinessException exception)
        {
            AuthModelStateLocalizer.AddBusinessError(ModelState, exception.Message, _localizer);
            return View(model);
        }
    }

    [AllowAnonymous]
    [HttpGet("register")]
    public async Task<IActionResult> Register(
        string? org = null,
        CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(null);

        ResolveAuthOrganizationContextResponse? organizationContext = await ResolveAuthOrganizationContextAsync(org, cancellationToken);
        if (organizationContext is null)
            return OrganizationContextRequired();

        RegisterViewModel model = new();
        ApplyOrganizationContext(model, organizationContext);
        await PopulateRegisterOptionsAsync(model, cancellationToken);

        return View(model);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model,
        CancellationToken cancellationToken)
    {
        ResolveAuthOrganizationContextResponse? organizationContext = await ResolveAuthOrganizationContextAsync(
            model.OrganizationSlug ?? model.OrganizationId?.ToString("D"),
            cancellationToken);
        if (organizationContext is null)
            return OrganizationContextRequired();

        ApplyOrganizationContext(model, organizationContext);

        if (!AuthModelStateLocalizer.ValidateRegister(model, ModelState, _localizer))
        {
            await PopulateRegisterOptionsAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            RegisteredResponse response = await _mediator.Send(
                new RegisterCommand
                {
                    Name = model.Name,
                    Surname = model.Surname,
                    Institution = model.Institution,
                    TitleId = model.TitleId,
                    OrganizationId = model.OrganizationId,
                    CountryId = model.CountryId,
                    StateId = model.StateId,
                    PhoneNumber = model.PhoneNumber,
                    Email = model.Email,
                    Password = model.Password,
                    ConfirmPassword = model.ConfirmPassword,
                    AcceptTerms = model.AcceptTerms
                },
                cancellationToken);

            string confirmationUrl = BuildAbsoluteActionUrl(
                nameof(ConfirmEmail),
                new
                {
                    culture = GetCurrentCulture(),
                    org = response.OrganizationSlug,
                    email = response.Email,
                    token = AuthTokenEncoder.Encode(response.EmailConfirmationToken)
                });

            BackOfficeEmailMessage confirmationMessage = await BuildEmailConfirmationMessageAsync(
                response.OrganizationId,
                response.Email,
                response.DisplayName,
                confirmationUrl,
                response.OrganizationName,
                response.OrganizationShortName,
                cancellationToken);

            await _mailQueueService.QueueAsync(
                new MailQueueRequest
                {
                    Message = confirmationMessage,
                    MailType = Symplify.BackOffice.Domain.Enums.MailMessageType.EmailConfirmation,
                    RelatedUserId = response.UserId,
                    ContainsSensitiveContent = true,
                    CreatedBy = response.UserId.ToString("D")
                },
                cancellationToken);

            model = new RegisterViewModel();
            ApplyOrganizationContext(model, organizationContext);
            await PopulateRegisterOptionsAsync(model, cancellationToken);
            ViewData["AuthSuccessMessage"] = _localizer.GetStringValueSafe(Symplify.BackOffice.Application.Features.Auth.Constants.AuthResourceKeys.RegisterSuccess);

            return View(model);
        }
        catch (BusinessException exception)
        {
            AuthModelStateLocalizer.AddBusinessError(ModelState, exception.Message, _localizer);
            await PopulateRegisterOptionsAsync(model, cancellationToken);
            return View(model);
        }
    }

    [AllowAnonymous]
    [HttpGet("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        string? org = null,
        CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(null);

        ResolveAuthOrganizationContextResponse? organizationContext = await ResolveAuthOrganizationContextAsync(org, cancellationToken);
        if (organizationContext is null)
            return OrganizationContextRequired();

        ForgotPasswordViewModel model = new();
        ApplyOrganizationContext(model, organizationContext);
        return View(model);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        ResolveAuthOrganizationContextResponse? organizationContext = await ResolveAuthOrganizationContextAsync(
            model.OrganizationSlug ?? model.OrganizationId?.ToString("D"),
            cancellationToken);
        if (organizationContext is null)
            return OrganizationContextRequired();

        ApplyOrganizationContext(model, organizationContext);

        if (!AuthModelStateLocalizer.ValidateForgotPassword(model, ModelState, _localizer))
            return View(model);

        ForgotPasswordResponse response = await _mediator.Send(
            new ForgotPasswordCommand
            {
                Email = model.Email
            },
            cancellationToken);

        if (response.TokenGenerated)
        {
            string resetUrl = BuildAbsoluteActionUrl(
                nameof(ResetPassword),
                new
                {
                    culture = GetCurrentCulture(),
                    org = organizationContext.Slug,
                    email = response.Email,
                    token = AuthTokenEncoder.Encode(response.Token)
                });

            BackOfficeEmailMessage resetMessage = await BuildResetPasswordMessageAsync(
                organizationContext.Id,
                response.Email,
                response.DisplayName,
                resetUrl,
                organizationContext.Name,
                organizationContext.ShortName,
                cancellationToken);

            await _mailQueueService.QueueAsync(
                new MailQueueRequest
                {
                    Message = resetMessage,
                    MailType = Symplify.BackOffice.Domain.Enums.MailMessageType.PasswordReset,
                    RelatedUserId = response.UserId,
                    ContainsSensitiveContent = true,
                    CreatedBy = response.UserId?.ToString("D") ?? "ForgotPassword"
                },
                cancellationToken);
        }

        model.MailSent = true;
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet("reset-password")]
    public async Task<IActionResult> ResetPassword(
        string? email = null,
        string? token = null,
        string? org = null,
        CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(null);

        ResolveAuthOrganizationContextResponse? organizationContext = await ResolveAuthOrganizationContextAsync(org, cancellationToken);
        if (organizationContext is null)
            return OrganizationContextRequired();

        ResetPasswordViewModel model = new()
        {
            Email = email ?? string.Empty,
            Token = token ?? string.Empty
        };

        ApplyOrganizationContext(model, organizationContext);
        return View(model);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        ResolveAuthOrganizationContextResponse? organizationContext = await ResolveAuthOrganizationContextAsync(
            model.OrganizationSlug ?? model.OrganizationId?.ToString("D"),
            cancellationToken);
        if (organizationContext is null)
            return OrganizationContextRequired();

        ApplyOrganizationContext(model, organizationContext);

        if (!AuthModelStateLocalizer.ValidateResetPassword(model, ModelState, _localizer))
            return View(model);

        try
        {
            await _mediator.Send(
                new ResetPasswordCommand
                {
                    Email = model.Email,
                    Token = AuthTokenEncoder.Decode(model.Token),
                    Password = model.Password,
                    ConfirmPassword = model.ConfirmPassword
                },
                cancellationToken);

            model.PasswordChanged = true;
            model.Password = string.Empty;
            model.ConfirmPassword = string.Empty;
            model.Token = string.Empty;
            return View(model);
        }
        catch (BusinessException exception)
        {
            AuthModelStateLocalizer.AddBusinessError(ModelState, exception.Message, _localizer);
            return View(model);
        }
    }

    [AllowAnonymous]
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        string? email = null,
        string? token = null,
        string? org = null,
        CancellationToken cancellationToken = default)
    {
        ResolveAuthOrganizationContextResponse? organizationContext = await ResolveAuthOrganizationContextAsync(org, cancellationToken);
        if (organizationContext is null)
            return OrganizationContextRequired();

        try
        {
            await _mediator.Send(
                new ConfirmEmailCommand
                {
                    Email = email ?? string.Empty,
                    Token = AuthTokenEncoder.Decode(token ?? string.Empty)
                },
                cancellationToken);

            return View(new ConfirmEmailViewModel
            {
                Success = true
            });
        }
        catch
        {
            return View(new ConfirmEmailViewModel
            {
                Success = false
            });
        }
    }

    [AllowAnonymous]
    [HttpGet("states")]
    public async Task<IActionResult> States(Guid countryId, CancellationToken cancellationToken)
    {
        List<AuthSelectOptionDto> states = await _mediator.Send(
            new GetStatesByCountryQuery
            {
                CountryId = countryId,
                Culture = GetCurrentCulture()
            },
            cancellationToken);

        return Json(states);
    }

    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        string? organizationSlug = User.FindFirst("OrganizationSlug")?.Value;

        await _cookieSignInService.SignOutAsync(HttpContext);

        Dictionary<string, object?> routeValues = new()
        {
            ["culture"] = GetCurrentCulture()
        };

        if (!string.IsNullOrWhiteSpace(organizationSlug))
            routeValues["org"] = organizationSlug;

        return RedirectToAction(nameof(Login), routeValues);
    }

    [AllowAnonymous]
    [HttpGet("access-denied")]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task<BackOfficeEmailMessage> BuildEmailConfirmationMessageAsync(
        Guid organizationId,
        string toEmail,
        string displayName,
        string confirmationUrl,
        string organizationName,
        string organizationShortName,
        CancellationToken cancellationToken)
    {
        string brandName = string.IsNullOrWhiteSpace(organizationShortName) ? "Symplify" : organizationShortName.Trim();
        string contextTitle = string.IsNullOrWhiteSpace(organizationName) ? brandName : organizationName.Trim();
        MailBrandingModel branding = await _mailBrandingResolver.ResolveForOrganizationAsync(
            organizationId,
            cancellationToken);
        branding.BrandName = brandName;
        branding.ContextTitle = contextTitle;
        branding.LogoAltText = contextTitle;

        RenderedSystemMailTemplate template = await _mailTemplateRenderer.RenderAsync(
            new SystemMailTemplateRenderRequest
            {
                Culture = GetCurrentCulture(),
                SubjectKey = SystemMailResourceKeys.EmailConfirmationSubject,
                TitleKey = SystemMailResourceKeys.EmailConfirmationTitle,
                BodyKey = SystemMailResourceKeys.EmailConfirmationBody,
                ActionTextKey = SystemMailResourceKeys.EmailConfirmationButton,
                ActionUrl = confirmationUrl,
                Branding = branding,
                Tokens = new Dictionary<string, string?>
                {
                    ["RecipientName"] = string.IsNullOrWhiteSpace(displayName) ? toEmail : displayName
                }
            },
            cancellationToken);

        return new BackOfficeEmailMessage
        {
            OrganizationId = organizationId,
            ToEmail = toEmail,
            ToName = displayName,
            Subject = template.Subject,
            HtmlBody = template.HtmlBody
        };
    }

    private async Task<BackOfficeEmailMessage> BuildResetPasswordMessageAsync(
        Guid organizationId,
        string toEmail,
        string displayName,
        string resetUrl,
        string organizationName,
        string organizationShortName,
        CancellationToken cancellationToken)
    {
        string brandName = string.IsNullOrWhiteSpace(organizationShortName) ? "Symplify" : organizationShortName.Trim();
        string contextTitle = string.IsNullOrWhiteSpace(organizationName) ? brandName : organizationName.Trim();
        MailBrandingModel branding = await _mailBrandingResolver.ResolveForOrganizationAsync(
            organizationId,
            cancellationToken);
        branding.BrandName = brandName;
        branding.ContextTitle = contextTitle;
        branding.LogoAltText = contextTitle;

        RenderedSystemMailTemplate template = await _mailTemplateRenderer.RenderAsync(
            new SystemMailTemplateRenderRequest
            {
                Culture = GetCurrentCulture(),
                SubjectKey = SystemMailResourceKeys.ResetPasswordSubject,
                TitleKey = SystemMailResourceKeys.ResetPasswordTitle,
                BodyKey = SystemMailResourceKeys.ResetPasswordBody,
                ActionTextKey = SystemMailResourceKeys.ResetPasswordButton,
                ActionUrl = resetUrl,
                Branding = branding,
                Tokens = new Dictionary<string, string?>
                {
                    ["RecipientName"] = string.IsNullOrWhiteSpace(displayName) ? toEmail : displayName
                }
            },
            cancellationToken);

        return new BackOfficeEmailMessage
        {
            OrganizationId = organizationId,
            ToEmail = toEmail,
            ToName = displayName,
            Subject = template.Subject,
            HtmlBody = template.HtmlBody
        };
    }

    private async Task PopulateRegisterOptionsAsync(RegisterViewModel model, CancellationToken cancellationToken)
    {
        GetRegisterOptionsResponse options = await _mediator.Send(
            new GetRegisterOptionsQuery
            {
                Culture = GetCurrentCulture()
            },
            cancellationToken);

        model.TitleOptions = ToSelectList(options.Titles, model.TitleId);
        model.CountryOptions = ToSelectList(options.Countries, model.CountryId);

        if (model.CountryId.HasValue && model.CountryId.Value != Guid.Empty)
        {
            List<AuthSelectOptionDto> states = await _mediator.Send(
                new GetStatesByCountryQuery
                {
                    CountryId = model.CountryId.Value,
                    Culture = GetCurrentCulture()
                },
                cancellationToken);

            model.StateOptions = ToSelectList(states, model.StateId);
        }
    }

    private async Task<ResolveAuthOrganizationContextResponse?> ResolveAuthOrganizationContextAsync(
        string? organization,
        CancellationToken cancellationToken)
    {
        string? returnUrl = Request.HasFormContentType
            ? Request.Form[nameof(LoginViewModel.ReturnUrl)].ToString()
            : Request.Query["ReturnUrl"].ToString();

        string? requestedOrganization = FirstNonEmpty(
            organization,
            Request.Query["org"].ToString(),
            Request.Query["organization"].ToString(),
            Request.Query["tenant"].ToString(),
            TryReadOrganizationFromUrl(returnUrl, "org"),
            TryReadOrganizationFromUrl(returnUrl, "organization"),
            TryReadOrganizationFromUrl(returnUrl, "tenant"),
            AuthOrganizationContextCookie.Read(HttpContext));

        return await _mediator.Send(
            new ResolveAuthOrganizationContextQuery
            {
                Organization = requestedOrganization,
                RequestHost = Request.Host.Value
            },
            cancellationToken);
    }

    private void ApplyOrganizationContext(LoginViewModel model, ResolveAuthOrganizationContextResponse? organization)
    {
        model.OrganizationId = organization?.Id;
        model.OrganizationSlug = organization?.Slug;
        model.OrganizationName = organization?.Name;
        model.OrganizationShortName = organization?.ShortName;

        string? lightLogoUrl = ResolveAuthLogoUrl(organization?.LogoLightPath)
            ?? ResolveAuthLogoUrl(organization?.LogoDarkPath);

        model.OrganizationLogoLightUrl = lightLogoUrl;
        model.OrganizationLogoDarkUrl = ResolveAuthLogoUrl(organization?.LogoDarkPath)
            ?? lightLogoUrl;

        PersistOrganizationContext(organization);
    }

    private void ApplyOrganizationContext(RegisterViewModel model, ResolveAuthOrganizationContextResponse? organization)
    {
        model.OrganizationId = organization?.Id;
        model.OrganizationSlug = organization?.Slug;
        model.OrganizationName = organization?.Name;
        model.OrganizationShortName = organization?.ShortName;

        string? lightLogoUrl = ResolveAuthLogoUrl(organization?.LogoLightPath)
            ?? ResolveAuthLogoUrl(organization?.LogoDarkPath);

        model.OrganizationLogoLightUrl = lightLogoUrl;
        model.OrganizationLogoDarkUrl = ResolveAuthLogoUrl(organization?.LogoDarkPath)
            ?? lightLogoUrl;

        PersistOrganizationContext(organization);
    }

    private void ApplyOrganizationContext(ForgotPasswordViewModel model, ResolveAuthOrganizationContextResponse? organization)
    {
        model.OrganizationId = organization?.Id;
        model.OrganizationSlug = organization?.Slug;
        model.OrganizationName = organization?.Name;
        model.OrganizationShortName = organization?.ShortName;

        string? lightLogoUrl = ResolveAuthLogoUrl(organization?.LogoLightPath)
            ?? ResolveAuthLogoUrl(organization?.LogoDarkPath);

        model.OrganizationLogoLightUrl = lightLogoUrl;
        model.OrganizationLogoDarkUrl = ResolveAuthLogoUrl(organization?.LogoDarkPath)
            ?? lightLogoUrl;

        PersistOrganizationContext(organization);
    }

    private void ApplyOrganizationContext(ResetPasswordViewModel model, ResolveAuthOrganizationContextResponse? organization)
    {
        model.OrganizationId = organization?.Id;
        model.OrganizationSlug = organization?.Slug;
        model.OrganizationName = organization?.Name;
        model.OrganizationShortName = organization?.ShortName;

        string? lightLogoUrl = ResolveAuthLogoUrl(organization?.LogoLightPath)
            ?? ResolveAuthLogoUrl(organization?.LogoDarkPath);

        model.OrganizationLogoLightUrl = lightLogoUrl;
        model.OrganizationLogoDarkUrl = ResolveAuthLogoUrl(organization?.LogoDarkPath)
            ?? lightLogoUrl;

        PersistOrganizationContext(organization);
    }

    private void PersistOrganizationContext(ResolveAuthOrganizationContextResponse? organization)
    {
        AuthOrganizationContextCookie.Append(HttpContext, organization?.Slug);
    }

    private static string? TryReadOrganizationFromUrl(string? url, string key)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        int queryStartIndex = url.IndexOf('?');
        if (queryStartIndex < 0 || queryStartIndex == url.Length - 1)
            return null;

        string query = url[(queryStartIndex + 1)..];
        foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] parts = pair.Split('=', 2);
            string decodedKey = System.Net.WebUtility.UrlDecode(parts[0]);

            if (!string.Equals(decodedKey, key, StringComparison.OrdinalIgnoreCase))
                continue;

            string? decodedValue = parts.Length > 1
                ? System.Net.WebUtility.UrlDecode(parts[1])
                : null;

            return AuthOrganizationContextCookie.NormalizeOrganizationKey(decodedValue);
        }

        return null;
    }

    private IActionResult OrganizationContextRequired()
    {
        return NotFound();
    }

    private static List<SelectListItem> ToSelectList(IEnumerable<AuthSelectOptionDto> options, Guid? selectedId)
    {
        string? selectedValue = selectedId.HasValue && selectedId.Value != Guid.Empty
            ? selectedId.Value.ToString("D")
            : null;

        return options
            .Select(item => new SelectListItem
            {
                Value = item.Value,
                Text = item.Text,
                Selected = string.Equals(item.Value, selectedValue, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(
            actionName: "Index",
            controllerName: "Home",
            routeValues: new { culture = GetCurrentCulture() });
    }

    private string BuildAbsoluteActionUrl(string actionName, object routeValues)
    {
        string? relativeUrl = Url.Action(
            actionName,
            "Auth",
            routeValues);

        if (string.IsNullOrWhiteSpace(relativeUrl))
            throw new InvalidOperationException("Auth action URL could not be generated.");

        return _publicUrlService.Build(relativeUrl);
    }

    private string? ResolveAuthLogoUrl(string? logoPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath))
            return null;

        string normalizedPath = logoPath.Trim();

        if (normalizedPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedPath;
        }

        if (normalizedPath.StartsWith('/'))
            return normalizedPath;

        string? bucketName = string.IsNullOrWhiteSpace(_objectStorageOptions.Buckets.CongressImages)
            ? null
            : _objectStorageOptions.Buckets.CongressImages.Trim();

        if (string.IsNullOrWhiteSpace(bucketName))
            return null;

        string objectName = string.Join(
            '/',
            normalizedPath
                .Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Uri.EscapeDataString));

        return $"/public-assets/{Uri.EscapeDataString(bucketName)}/{objectName}";
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }

    private string GetCurrentCulture()
    {
        string? routeCulture = RouteData.Values["culture"]?.ToString();
        return string.IsNullOrWhiteSpace(routeCulture) ? "tr-TR" : routeCulture;
    }
}
