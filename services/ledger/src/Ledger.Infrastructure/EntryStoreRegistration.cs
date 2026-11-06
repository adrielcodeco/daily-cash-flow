// @anchors
//   code: LDREG
//   layer: infrastructure
//   updated_at: 2026-11-06
using Ledger.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ledger.Infrastructure;

public static class EntryStoreRegistration
{
    /// <summary>The cache is off by default. Without targets, there is no connection to Redis.</summary>
    public static void AddEntryStore(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<PostgresEntryStore>();

        var policy = CachePolicy.Parse(builder.Configuration["Cache:Targets"]);
        if (policy.IsEmpty)
        {
            builder.Services.AddSingleton<IEntryStore>(services => services.GetRequiredService<PostgresEntryStore>());
            return;
        }

        var connection = builder.Configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("ConnectionStrings:Redis is required when the cache is on.");
        var cache = RedisQueryCache.Connect(connection);
        builder.Services.AddSingleton<IEntryStore>(services =>
            new CachedEntryStore(services.GetRequiredService<PostgresEntryStore>(), cache, policy));
    }
}
