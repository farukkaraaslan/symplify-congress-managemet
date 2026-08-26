using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Symplify.BackOffice.Application.Features.ParticipationCertificates.Services;

namespace Symplify.BackOffice.Infrastructure.Email;

/// <summary>
/// ParticipationCertificates tablosunda QueueRequested durumunda bekleyen kayıtları
/// MailOutboxMessages tablosuna güvenli public belge linkiyle hazırlayan arka plan worker'ıdır.
/// SMTP gönderimini MailOutboxDispatcherHostedService gerçekleştirir.
/// </summary>
public sealed class ParticipationCertificateEmailQueueHostedService : BackgroundService
{
    private const int BatchSize = 100;
    private const int MaxBatchesPerCycle = 10;

    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan BusyDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ErrorDelay = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ParticipationCertificateEmailQueueHostedService> _logger;

    public ParticipationCertificateEmailQueueHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ParticipationCertificateEmailQueueHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            int totalProcessed = 0;

            try
            {
                for (int batchNumber = 0;
                     batchNumber < MaxBatchesPerCycle && !stoppingToken.IsCancellationRequested;
                     batchNumber++)
                {
                    await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
                    IParticipationCertificateService service =
                        scope.ServiceProvider.GetRequiredService<IParticipationCertificateService>();

                    int processed = await service.ProcessRequestedEmailQueueBatchAsync(
                        BatchSize,
                        stoppingToken);

                    totalProcessed += processed;

                    if (processed == 0)
                        break;
                }

                if (totalProcessed > 0)
                {
                    _logger.LogInformation(
                        "Participation certificate email queue prepared {ProcessedCount} certificate(s).",
                        totalProcessed);
                }

                await Task.Delay(totalProcessed > 0 ? BusyDelay : IdleDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Participation certificate email queue worker failed. It will retry automatically.");

                try
                {
                    await Task.Delay(ErrorDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
