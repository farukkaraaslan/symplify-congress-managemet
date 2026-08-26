using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Symplify.BackOffice.Application;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Infrastructure.DependencyInjection;
using Symplify.BackOffice.Infrastructure.Email;
using Symplify.BackOffice.Persistence.DependencyInjection;
using Symplify.BackOffice.WebUI.Extensions;
using Symplify.BackOffice.WebUI.Middleware;
using Symplify.BackOffice.WebUI.Services.Auth;
using Symplify.BackOffice.WebUI.Services.Localization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = builder.Configuration;
IWebHostEnvironment environment = builder.Environment;

builder.Services.AddHttpContextAccessor();

builder.Services
    .AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = false;
    options.LowercaseQueryStrings = false;
});

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".Symplify.BackOffice.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromHours(2);
});

builder.Services.AddScoped<ICurrentCultureProvider, HttpContextCurrentCultureProvider>();

builder.Services.AddBackOfficeApplicationServices();
builder.Services.AddBackOfficePersistenceServices(configuration);
builder.Services.AddBackOfficeDbViewLocalization();
builder.Services.AddBackOfficeInfrastructureServices(configuration);
builder.Services.AddHostedService<ParticipationCertificateEmailQueueHostedService>();
builder.Services.AddBackOfficeMinioObjectStorage(builder.Configuration);
builder.Services.AddBackOfficeWebAuthentication(configuration);

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("public-certificate", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
});

WebApplication app = builder.Build();

await app.InitializeBackOfficeDatabaseAsync();

if (!environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();

app.UseMiddleware<RouteCultureMiddleware>();

app.UseSession();
app.UseAuthentication();
app.UseMiddleware<BackOfficeAuthorizationExceptionMiddleware>();
app.UseMiddleware<RequirePhoneNumberMiddleware>();
app.UseAuthorization();

app.MapGet("/", context =>
{
    bool isAuthenticated = context.User.Identity?.IsAuthenticated == true;
    string redirectUrl = isAuthenticated
        ? "/tr-TR/home/index"
        : BackOfficeAuthRedirectUrlBuilder.BuildLoginRedirectUrl(context, "/tr-TR/auth/login");

    context.Response.Redirect(redirectUrl);
    return Task.CompletedTask;
});

app.MapControllerRoute(
    name: "localized-default",
    pattern: "{culture=tr-TR}/{controller=Auth}/{action=Login}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
