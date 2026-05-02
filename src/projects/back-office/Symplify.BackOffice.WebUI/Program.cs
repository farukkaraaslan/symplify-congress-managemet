using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Razor;
using Symplify.BackOffice.Application;
using Symplify.BackOffice.Application.Services.Localization;
using Symplify.BackOffice.Infrastructure.DependencyInjection;
using Symplify.BackOffice.Persistence.DependencyInjection;
using Symplify.BackOffice.WebUI.Extensions;
using Symplify.BackOffice.WebUI.Middleware;
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
builder.Services.AddBackOfficeInfrastructureServices();
builder.Services.AddBackOfficeWebAuthentication(configuration);

builder.Services.AddAuthorization();

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
    app.UseExceptionHandler("/error");
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

app.UseMiddleware<RouteCultureMiddleware>();

app.UseSession();
app.UseAuthentication();
app.UseMiddleware<BackOfficeAuthorizationExceptionMiddleware>();
app.UseAuthorization();

app.MapGet("/", context =>
{
    context.Response.Redirect("/tr-TR/auth/login");
    return Task.CompletedTask;
});

app.MapControllerRoute(
    name: "localized-default",
    pattern: "{culture=tr-TR}/{controller=Auth}/{action=Login}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
