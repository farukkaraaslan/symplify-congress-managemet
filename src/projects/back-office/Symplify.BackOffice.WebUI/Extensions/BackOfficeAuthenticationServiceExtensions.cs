using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Symplify.BackOffice.Domain.Identity;
using Symplify.BackOffice.Persistence.Contexts;
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

        int expireHours = cookieSection.GetValue<int?>("ExpireHours") ?? 8;

        bool slidingExpiration = cookieSection.GetValue<bool?>("SlidingExpiration") ?? true;

        services.AddMemoryCache();

        services
            .AddIdentity<AppUser, AppRole>(options =>
            {
                options.User.RequireUniqueEmail = true;

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

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = cookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;

            options.LoginPath = loginPath;
            options.AccessDeniedPath = accessDeniedPath;

            options.ExpireTimeSpan = TimeSpan.FromHours(expireHours);
            options.SlidingExpiration = slidingExpiration;
        });

        services.AddScoped<IBackOfficeCookieSignInService, BackOfficeCookieSignInService>();
        services.AddScoped<IClaimsTransformation, BackOfficeClaimsTransformation>();

        return services;
    }
}