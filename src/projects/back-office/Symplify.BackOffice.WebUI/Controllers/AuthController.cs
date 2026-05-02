using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Application.Features.Auth.Commands.Login;
using Symplify.BackOffice.WebUI.Models.Auth;
using Symplify.BackOffice.WebUI.Services.Authentication;

namespace Symplify.BackOffice.WebUI.Controllers;

[AllowAnonymous]
[Route("{culture?}/auth")]
public sealed class AuthController : Controller
{
    private readonly IMediator _mediator;
    private readonly IBackOfficeCookieSignInService _cookieSignInService;

    public AuthController(
        IMediator mediator,
        IBackOfficeCookieSignInService cookieSignInService)
    {
        _mediator = mediator;
        _cookieSignInService = cookieSignInService;
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            LoggedInResponse response = await _mediator.Send(
                new LoginCommand
                {
                    Email = model.Email,
                    Password = model.Password
                },
                cancellationToken);

            await _cookieSignInService.SignInAsync(
                  HttpContext,
                  response.User,
                  model.RememberMe,
                  cancellationToken);

            return RedirectToLocal(model.ReturnUrl);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "E-posta adresi veya şifre hatalı.");
            return View(model);
        }
    }

    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _cookieSignInService.SignOutAsync(HttpContext);

        return RedirectToAction(
            nameof(Login),
            new { culture = GetCurrentCulture() });
    }

    [HttpGet("access-denied")]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(
            actionName: "Index",
            controllerName: "Topics",
            routeValues: new { culture = GetCurrentCulture() });
    }

    private string GetCurrentCulture()
    {
        string? routeCulture = RouteData.Values["culture"]?.ToString();
        return string.IsNullOrWhiteSpace(routeCulture) ? "tr-TR" : routeCulture;
    }
}
