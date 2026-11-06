// @anchors
//   code: LDOWK
//   layer: host
//   updated_at: 2026-11-05
using Ledger.Core;

namespace Ledger.Publisher;

public sealed class OutboxWorker(OutboxRelay relay, ILogger<OutboxWorker> logger) : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan IdleDelay = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan FailureDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var published = await relay.PublishPendingAsync(BatchSize, stoppingToken);
                if (published < BatchSize)
                {
                    await Task.Delay(IdleDelay, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Outbox relay failed. Retrying.");
                await Task.Delay(FailureDelay, stoppingToken);
            }
        }
    }
}
