// @anchors
//   code: LDTEL
//   layer: infrastructure
//   updated_at: 2026-11-06
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Ledger.Infrastructure;

public static class ServiceTelemetry
{
    /// <summary>
    /// Export runs in the background with a bounded queue, so a collector outage drops telemetry and never blocks a request.
    /// The endpoint and the sampler come from the standard OTEL_* variables.
    /// </summary>
    public static IServiceCollection AddServiceTelemetry(
        this IServiceCollection services,
        string serviceName,
        Action<TracerProviderBuilder>? configureTracing = null,
        Action<MeterProviderBuilder>? configureMetrics = null)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddSource(OutboxTelemetry.SourceName).AddNpgsql().AddOtlpExporter();
                configureTracing?.Invoke(tracing);
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(OutboxTelemetry.SourceName).AddOtlpExporter();
                configureMetrics?.Invoke(metrics);
            });

        return services;
    }
}
