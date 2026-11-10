// @anchors
//   code: CESCD
//   ref: CESTR
//   updated_at: 2026-11-09
namespace Ledger.Core;

/// <summary>Puts the optional cache in front of the store, so the use cases do not know it exists.</summary>
public sealed class CachedEntryStore(IEntryStore inner, IQueryCache cache, CachePolicy policy) : IEntryStore
{
    public const string GetRoute = "entries.get";
    public const string ListRoute = "entries.list";
    public const string Table = "ledger_entry";

    /// <summary>True when the policy names a route or the table of this store; targets of other services do not count.</summary>
    public static bool IsOn(CachePolicy policy) => policy.Covers(GetRoute, Table) || policy.Covers(ListRoute, Table);

    public async Task<AddOutcome> AddAsync(LedgerEntry entry, EntryRecorded recorded, IdempotencyClaim claim, CancellationToken cancellationToken)
    {
        var outcome = await inner.AddAsync(entry, recorded, claim, cancellationToken);
        if (outcome == AddOutcome.Added && IsOn(policy))
        {
            await TryInvalidateAsync(entry.MerchantId, cancellationToken);
        }

        return outcome;
    }

    public Task<LedgerEntry?> FindAsync(Guid merchantId, Guid id, CancellationToken cancellationToken) =>
        policy.Covers(GetRoute, Table)
            ? ReadThroughAsync(merchantId, $"entry:{id}", () => inner.FindAsync(merchantId, id, cancellationToken), cancellationToken)
            : inner.FindAsync(merchantId, id, cancellationToken);

    public Task<ClaimedEntry?> FindClaimAsync(Guid merchantId, string key, CancellationToken cancellationToken) =>
        inner.FindClaimAsync(merchantId, key, cancellationToken);

    public async Task<EntryPage> ListAsync(Guid merchantId, DateOnly date, Guid? cursor, int limit, CancellationToken cancellationToken)
    {
        if (!policy.Covers(ListRoute, Table))
        {
            return await inner.ListAsync(merchantId, date, cursor, limit, cancellationToken);
        }

        var page = await ReadThroughAsync<EntryPage>(
            merchantId,
            $"list:{date:O}:{cursor}:{limit}",
            async () => await inner.ListAsync(merchantId, date, cursor, limit, cancellationToken),
            cancellationToken);
        return page!;
    }

    public Task<IReadOnlyList<LedgerEntry>> ReadBatchAsync(Guid? after, int batchSize, CancellationToken cancellationToken) =>
        inner.ReadBatchAsync(after, batchSize, cancellationToken);

    public Task RequeueAsync(IReadOnlyList<EntryRecorded> recorded, CancellationToken cancellationToken) =>
        inner.RequeueAsync(recorded, cancellationToken);

    private async Task<T?> ReadThroughAsync<T>(Guid merchantId, string key, Func<Task<T?>> load, CancellationToken cancellationToken)
        where T : class
    {
        var lookup = await TryReadAsync<T>(merchantId, key, cancellationToken);
        if (lookup?.Value is not null)
        {
            return lookup.Value;
        }

        var value = await load();
        if (lookup is not null && value is not null)
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
