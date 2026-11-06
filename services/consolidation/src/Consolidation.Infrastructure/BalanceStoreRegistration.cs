// @anchors
//   code: CNREG
//   layer: infrastructure
//   updated_at: 2026-11-06
using Consolidation.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Consolidation.Infrastructure;

public static class BalanceStoreRegistration
{
    /// <summary>The cache is off by default. Without targets, there is no connection to Redis.</summary>
    public static void AddBalanceStore(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<PostgresBalanceStore>();

        var policy = CachePolicy.Parse(builder.Configuration["Cache:Targets"]);
        if (policy.IsEmpty)
        {
            builder.Services.AddSingleton<IBalanceStore>(services => services.GetRequiredService<PostgresBalanceStore>());
            return;
        }

        var connection = builder.Configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("ConnectionStrings:Redis is required when the cache is on.");
        var cache = RedisQueryCache.Connect(connection);
        builder.Services.AddSingleton<IBalanceStore>(services =>
            new CachedBalanceStore(services.GetRequiredService<PostgresBalanceStore>(), cache, policy));
    }
}
