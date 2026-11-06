// @anchors
//   code: OBTEL
//   layer: infrastructure
//   updated_at: 2026-11-06
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Ledger.Infrastructure;

public static class OutboxTelemetry
{
    public const string SourceName = "Ledger.Outbox";

    public static readonly ActivitySource Source = new(SourceName);

    private static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> Published = Meter.CreateCounter<long>("outbox.published.events");

    private static long pendingEvents;
    private static double oldestAgeSeconds;

    static OutboxTelemetry()
    {
        Meter.CreateObservableGauge("outbox.pending.events", () => Interlocked.Read(ref pendingEvents));
        Meter.CreateObservableGauge("outbox.oldest.age", () => Volatile.Read(ref oldestAgeSeconds), unit: "s");
    }

    public static void RecordBacklog(long pending, double oldestAge)
    {
        Interlocked.Exchange(ref pendingEvents, pending);
        Volatile.Write(ref oldestAgeSeconds, oldestAge);
    }
}
