using Symplify.Portal.WebUI.Options;
using Symplify.Portal.WebUI.Services.PublicSite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

builder.Services.Configure<PublicApiOptions>(builder.Configuration.GetSection(PublicApiOptions.SectionName));

builder.Services.AddScoped<IPortalCultureService, PortalCultureService>();
builder.Services.AddScoped<IPublicSiteService, PublicSiteService>();

builder.Services.AddHttpClient<IPublicSiteApiClient, PublicSiteApiClient>((serviceProvider, client) =>
{
    PublicApiOptions options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<PublicApiOptions>>()
        .Value;

    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
