// @anchors
//   code: RDBCH
//   ref: GTBAL
//   updated_at: 2026-11-05
using System.Text.Json;
using Consolidation.Core;
using StackExchange.Redis;

namespace Consolidation.Infrastructure;

/// <summary>
/// Keys carry a version per merchant, so one increment invalidates every cached day.
/// A backdated entry changes the opening of all later days, which rules out per-day invalidation.
/// While disconnected, every call is skipped: waiting for Redis timeouts would make a cache outage slower than no cache.
/// </summary>
public sealed class RedisBalanceCache(IConnectionMultiplexer redis) : IBalanceCache
{
    private static readonly TimeSpan TimeToLive = TimeSpan.FromSeconds(60);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static RedisBalanceCache Connect(string configuration) =>
        new(ConnectionMultiplexer.Connect(configuration));

    public async Task<DailyBalance?> GetAsync(Guid merchantId, DateOnly date, CancellationToken cancellationToken)
    {
        if (!redis.IsConnected)
        {
            return null;
        }

        var database = redis.GetDatabase();
        var cached = await database.StringGetAsync(await BalanceKeyAsync(database, merchantId, date));
        return cached.HasValue ? JsonSerializer.Deserialize<DailyBalance>(cached.ToString(), Json) : null;
    }

    public async Task SetAsync(Guid merchantId, DailyBalance balance, CancellationToken cancellationToken)
    {
        if (!redis.IsConnected)
        {
            return;
        }

        var database = redis.GetDatabase();
        var key = await BalanceKeyAsync(database, merchantId, balance.Date);
        await database.StringSetAsync(key, JsonSerializer.Serialize(balance, Json), TimeToLive);
    }

    public Task InvalidateAsync(Guid merchantId, CancellationToken cancellationToken) =>
        redis.IsConnected ? redis.GetDatabase().StringIncrementAsync(VersionKey(merchantId)) : Task.CompletedTask;

    private static async Task<RedisKey> BalanceKeyAsync(IDatabase database, Guid merchantId, DateOnly date)
    {
        var version = await database.StringGetAsync(VersionKey(merchantId));
        return $"balance:{merchantId}:{(version.HasValue ? version.ToString() : "0")}:{date:O}";
    }

    private static RedisKey VersionKey(Guid merchantId) => $"balance-version:{merchantId}";
}
