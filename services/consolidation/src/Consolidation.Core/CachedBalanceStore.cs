// @anchors
//   code: CBSCD
//   ref: CBSTR
//   updated_at: 2026-11-09
namespace Consolidation.Core;

/// <summary>Puts the optional cache in front of the store, so the use cases do not know it exists.</summary>
public sealed class CachedBalanceStore(IBalanceStore inner, IQueryCache cache, CachePolicy policy) : IBalanceStore
{
    public const string DailyRoute = "balances.daily";
    public const string PeriodRoute = "balances.period";
    public const string Table = "daily_balance";

    /// <summary>True when the policy names a route or the table of this store; targets of other services do not count.</summary>
    public static bool IsOn(CachePolicy policy) => policy.Covers(DailyRoute, Table) || policy.Covers(PeriodRoute, Table);

    public async Task<bool> ApplyOnceAsync(BalanceChange change, CancellationToken cancellationToken)
    {
        var applied = await inner.ApplyOnceAsync(change, cancellationToken);
        if (applied && IsOn(policy))
        {
            await TryInvalidateAsync(change.MerchantId, cancellationToken);
        }

        return applied;
    }

    public Task<DailyBalance> GetDailyAsync(Guid merchantId, DateOnly date, CancellationToken cancellationToken) =>
        policy.Covers(DailyRoute, Table)
            ? ReadThroughAsync(merchantId, $"daily:{date:O}", () => inner.GetDailyAsync(merchantId, date, cancellationToken), cancellationToken)
            : inner.GetDailyAsync(merchantId, date, cancellationToken);

    public Task<PeriodTotals> GetPeriodAsync(Guid merchantId, DateOnly start, DateOnly end, CancellationToken cancellationToken) =>
        policy.Covers(PeriodRoute, Table)
            ? ReadThroughAsync(merchantId, $"period:{start:O}:{end:O}", () => inner.GetPeriodAsync(merchantId, start, end, cancellationToken), cancellationToken)
            : inner.GetPeriodAsync(merchantId, start, end, cancellationToken);

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        await inner.ResetAsync(cancellationToken);
        if (IsOn(policy))
        {
            // Unlike a single event, a reset that leaves the cache behind would serve balances that no longer exist.
            await cache.InvalidateAllAsync(cancellationToken);
        }
    }

    private async Task<T> ReadThroughAsync<T>(Guid merchantId, string key, Func<Task<T>> load, CancellationToken cancellationToken)
        where T : class
    {
        var lookup = await TryReadAsync<T>(merchantId, key, cancellationToken);
        if (lookup?.Value is { } cached)
        {
            return cached;
        }

        var value = await load();
        if (lookup is not null)
        {
            await TryWriteAsync(merchantId, key, lookup.Version, value, cancellationToken);
        }

        return value;
    }

    private async Task<CacheLookup<T>?> TryReadAsync<T>(Guid merchantId, string key, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            return await cache.GetAsync<T>(merchantId, key, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return null;
        }
    }

    private async Task TryWriteAsync<T>(Guid merchantId, string key, string version, T value, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            await cache.SetAsync(merchantId, key, version, value, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The cache only improves latency, so the query still succeeds without it.
        }
    }

    private async Task TryInvalidateAsync(Guid merchantId, CancellationToken cancellationToken)
    {
        try
        {
            await cache.InvalidateAsync(merchantId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The cached value expires on its own, which is better than failing the write.
        }
    }
}
