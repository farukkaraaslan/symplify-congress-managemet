using Microsoft.Extensions.Options;
using Symplify.Api.Application.DependencyInjection;
using Symplify.Api.Application.Features.PublicSite.Contexts;
using Symplify.Api.Persistence.DependencyInjection;
using Symplify.Api.Persistence.PublicSite;
using Symplify.Api.WebAPI.Middleware;
using Symplify.Api.WebAPI.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddHttpClient("PublicAssetsProxy")
    .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
    {
        PublicAssetOptions options = serviceProvider
            .GetRequiredService<IOptions<PublicAssetOptions>>()
            .Value;

        HttpClientHandler handler = new();

        if (options.AllowInvalidUpstreamCertificate)
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        return handler;
    });

builder.Services.AddSymplifyApiApplication();
builder.Services.AddSymplifyApiPersistence(builder.Configuration);
builder.Services.AddScoped<IPublicApiContextAccessor, HttpContextPublicApiContextAccessor>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<PublicApiKeyAuthenticationMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();
