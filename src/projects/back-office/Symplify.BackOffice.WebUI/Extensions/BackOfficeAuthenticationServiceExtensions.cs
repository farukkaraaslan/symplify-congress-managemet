using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Symplify.BackOffice.Domain.Identity;
using Symplify.BackOffice.Persistence.Contexts;
using Symplify.BackOffice.WebUI.Authorization;
using Symplify.BackOffice.WebUI.Services.Auth;
using Symplify.BackOffice.WebUI.Services.Authentication;

namespace Symplify.BackOffice.WebUI.Extensions;

public static class BackOfficeAuthenticationServiceExtensions
{
    public static IServiceCollection AddBackOfficeWebAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        IConfigurationSection cookieSection = configuration.GetSection("Authentication:Cookie");

        string cookieName = cookieSection.GetValue<string>("Name")
            ?? ".Symplify.BackOffice.Auth";

        string loginPath = cookieSection.GetValue<string>("LoginPath")
            ?? "/tr-TR/auth/login";

        string accessDeniedPath = cookieSection.GetValue<string>("AccessDeniedPath")
            ?? "/tr-TR/auth/access-denied";

        int expireMinutes = cookieSection.GetValue<int?>("ExpireMinutes") ?? 30;
        expireMinutes = Math.Clamp(expireMinutes, 1, 1440);

        bool slidingExpiration = cookieSection.GetValue<bool?>("SlidingExpiration") ?? true;

        services.AddMemoryCache();
        services.AddScoped<BackOfficePermissionAuthorizationFilter>();
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.AddService<BackOfficePermissionAuthorizationFilter>();
        });

        services
            .AddIdentity<AppUser, AppRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<BackOfficeDbContext>()
            .AddDefaultTokenProviders();

        int tokenLifespanHours = configuration.GetValue<int?>("Authentication:Tokens:LifespanHours") ?? 2;
        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromHours(Math.Clamp(tokenLifespanHours, 1, 24));
        });

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = cookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;

            options.LoginPath = loginPath;
            options.AccessDeniedPath = accessDeniedPath;

            options.ExpireTimeSpan = TimeSpan.FromMinutes(expireMinutes);
            options.SlidingExpiration = slidingExpiration;

            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = context =>
                {
                    string redirectUrl = BackOfficeAuthRedirectUrlBuilder.BuildLoginRedirectUrl(
                        context.HttpContext,
                        loginPath);

                    context.Response.Redirect(redirectUrl);
                    return Task.CompletedTask;
                },
                OnRedirectToAccessDenied = context =>
                {
                    string redirectUrl = BackOfficeAuthRedirectUrlBuilder.BuildAccessDeniedRedirectUrl(
                        context.HttpContext,
                        accessDeniedPath);

                    context.Response.Redirect(redirectUrl);
                    return Task.CompletedTask;
                }
            };
        });

        services.AddScoped<IBackOfficeCookieSignInService, BackOfficeCookieSignInService>();
        services.AddScoped<IClaimsTransformation, BackOfficeClaimsTransformation>();

        return services;
    }
}
