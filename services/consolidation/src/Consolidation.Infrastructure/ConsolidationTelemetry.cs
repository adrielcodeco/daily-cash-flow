// @anchors
//   code: CNOTL
//   layer: infrastructure
//   updated_at: 2026-11-06
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Consolidation.Infrastructure;

public static class ConsolidationTelemetry
{
    public const string Name = "Consolidation";

    public static readonly ActivitySource Source = new(Name);

    private static readonly Meter Meter = new(Name);

    public static readonly Counter<long> ProcessedEvents = Meter.CreateCounter<long>("consumer.processed.events");

    public static readonly Histogram<double> BalanceLag = Meter.CreateHistogram<double>("balance.lag", unit: "s");

    public static readonly Counter<long> CacheRequests = Meter.CreateCounter<long>("cache.requests");
}
