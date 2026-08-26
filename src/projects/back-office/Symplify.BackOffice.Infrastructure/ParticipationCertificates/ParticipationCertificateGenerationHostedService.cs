using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;

namespace Symplify.BackOffice.Infrastructure.ParticipationCertificates;

public sealed class ParticipationCertificateGenerationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ParticipationCertificateGenerationHostedService> _logger;

    public ParticipationCertificateGenerationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ParticipationCertificateGenerationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            bool processedJob = false;
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                IParticipationCertificateService service = scope.ServiceProvider.GetRequiredService<IParticipationCertificateService>();
                processedJob = await service.ProcessNextGenerationJobAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Participation certificate generation worker cycle failed.");
            }

            if (!processedJob)
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
}
