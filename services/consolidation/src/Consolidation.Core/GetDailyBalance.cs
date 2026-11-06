// @anchors
//   code: GTBCD
//   ref: GTBAL
//   updated_at: 2026-11-05
namespace Consolidation.Core;

public sealed class GetDailyBalance(IBalanceStore store, IBalanceCache cache)
{
    public async Task<DailyBalance> ExecuteAsync(Guid merchantId, DateOnly date, CancellationToken cancellationToken)
    {
        var cached = await TryReadCacheAsync(merchantId, date, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var balance = await store.GetDailyAsync(merchantId, date, cancellationToken);
        await TryWriteCacheAsync(merchantId, balance, cancellationToken);
        return balance;
    }

    private async Task<DailyBalance?> TryReadCacheAsync(Guid merchantId, DateOnly date, CancellationToken cancellationToken)
    {
        try
        {
            return await cache.GetAsync(merchantId, date, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return null;
        }
    }

    private async Task TryWriteCacheAsync(Guid merchantId, DailyBalance balance, CancellationToken cancellationToken)
    {
        try
        {
            await cache.SetAsync(merchantId, balance, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The cache only improves latency, so the query still succeeds without it.
        }
    }
}
