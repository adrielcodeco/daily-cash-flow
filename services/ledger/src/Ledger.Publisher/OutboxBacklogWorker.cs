// @anchors
//   code: OBBWK
//   layer: host
//   updated_at: 2026-11-06
using Ledger.Infrastructure;

namespace Ledger.Publisher;

/// <summary>Reads the backlog on its own schedule, so the metric keeps moving when the relay is stuck.</summary>
public sealed class OutboxBacklogWorker(PostgresOutbox outbox, ILogger<OutboxBacklogWorker> logger) : BackgroundService
{
    private static readonly TimeSpan ReadInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(ReadInterval);
        do
        {
            try
            {
                var (pending, oldestAge) = await outbox.ReadBacklogAsync(stoppingToken);
                OutboxTelemetry.RecordBacklog(pending, oldestAge);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "The outbox backlog could not be read.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
