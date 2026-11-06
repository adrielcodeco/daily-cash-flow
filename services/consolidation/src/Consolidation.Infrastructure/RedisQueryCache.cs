// @anchors
//   code: RDQCH
//   ref: CBSTR
//   updated_at: 2026-11-06
using System.Diagnostics.Metrics;
using System.Text.Json;
using Consolidation.Core;
using StackExchange.Redis;

namespace Consolidation.Infrastructure;

/// <summary>
/// Keys carry a version per merchant, so one increment invalidates every cached value of that merchant.
/// While disconnected, every call is skipped: waiting for Redis timeouts would make a cache outage slower than no cache.
/// </summary>
public sealed class RedisQueryCache(IConnectionMultiplexer redis) : IQueryCache
{
    public const string MeterName = "Consolidation.Cache";

    private const string Prefix = "balance";

    private static readonly TimeSpan TimeToLive = TimeSpan.FromSeconds(60);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Requests = Meter.CreateCounter<long>("cache.requests");

    public static RedisQueryCache Connect(string configuration) =>
        new(ConnectionMultiplexer.Connect(configuration));

    public async Task<CacheLookup<T>> GetAsync<T>(Guid merchantId, string key, CancellationToken cancellationToken)
        where T : class
    {
        if (!redis.IsConnected)
        {
            Count("disconnected");
            throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "The cache is disconnected.");
        }

        var database = redis.GetDatabase();
        var stored = await database.StringGetAsync(VersionKey(merchantId));
        var version = stored.HasValue ? stored.ToString() : "0";

        var cached = await database.StringGetAsync(ValueKey(merchantId, version, key));
        Count(cached.HasValue ? "hit" : "miss");
        return new CacheLookup<T>(cached.HasValue ? JsonSerializer.Deserialize<T>(cached.ToString(), Json) : null, version);
    }

    public async Task SetAsync<T>(Guid merchantId, string key, string version, T value, CancellationToken cancellationToken)
        where T : class
    {
        if (redis.IsConnected)
        {
            await redis.GetDatabase().StringSetAsync(ValueKey(merchantId, version, key), JsonSerializer.Serialize(value, Json), TimeToLive);
        }
    }

    public Task InvalidateAsync(Guid merchantId, CancellationToken cancellationToken) =>
        redis.IsConnected ? redis.GetDatabase().StringIncrementAsync(VersionKey(merchantId)) : Task.CompletedTask;

    private static void Count(string result) => Requests.Add(1, new KeyValuePair<string, object?>("result", result));

    private static RedisKey ValueKey(Guid merchantId, string version, string key) => $"{Prefix}:{merchantId}:{version}:{key}";

    private static RedisKey VersionKey(Guid merchantId) => $"{Prefix}-version:{merchantId}";
}
