// @anchors
//   code: LDFKS
//   layer: test
//   updated_at: 2026-11-06
using Ledger.Core;

namespace Ledger.Tests;

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class InMemoryEntryStore : IEntryStore
{
    private readonly Dictionary<(Guid, string), ClaimedEntry> claims = [];

    public List<LedgerEntry> Entries { get; } = [];

    public List<EntryRecorded> Events { get; } = [];

    public List<EntryRecorded> Requeued { get; } = [];

    public int Finds { get; private set; }

    public int Lists { get; private set; }

    /// <summary>Simulates an invalidation that happens while the query runs.</summary>
    public Action? DuringQuery { get; set; }

    /// <summary>Simulates a concurrent call that claims the key after the first lookup.</summary>
    public (string Key, ClaimedEntry Claim, LedgerEntry Entry)? RacingWinner { get; set; }

    public Task<AddOutcome> AddAsync(LedgerEntry entry, EntryRecorded recorded, IdempotencyClaim claim, CancellationToken cancellationToken)
    {
        if (RacingWinner is { } winner && winner.Key == claim.Key)
        {
            Entries.Add(winner.Entry);
            claims[(entry.MerchantId, claim.Key)] = winner.Claim;
            return Task.FromResult(AddOutcome.KeyAlreadyClaimed);
        }

        if (entry.ReversalOf is not null && Entries.Any(e => e.ReversalOf == entry.ReversalOf))
        {
            return Task.FromResult(AddOutcome.AlreadyReversed);
        }

        Entries.Add(entry);
        Events.Add(recorded);
        claims[(entry.MerchantId, claim.Key)] = new ClaimedEntry(claim.RequestHash, entry.Id);
        return Task.FromResult(AddOutcome.Added);
    }

    public Task<LedgerEntry?> FindAsync(Guid merchantId, Guid id, CancellationToken cancellationToken)
    {
        Finds++;
        DuringQuery?.Invoke();
        return Task.FromResult(Entries.FirstOrDefault(e => e.Id == id && e.MerchantId == merchantId));
    }

    public Task<ClaimedEntry?> FindClaimAsync(Guid merchantId, string key, CancellationToken cancellationToken) =>
        Task.FromResult(claims.GetValueOrDefault((merchantId, key)));

    public Task<EntryPage> ListAsync(Guid merchantId, DateOnly date, Guid? cursor, int limit, CancellationToken cancellationToken)
    {
        Lists++;
        var items = Entries
            .Where(e => e.MerchantId == merchantId && e.AccountingDate == date)
            .OrderBy(e => e.Id)
            .SkipWhile(e => cursor is not null && e.Id.CompareTo(cursor.Value) <= 0)
            .ToList();
        var page = items.Take(limit).ToList();
        return Task.FromResult(new EntryPage(page, items.Count > limit ? page[^1].Id : null));
    }

    public Task<IReadOnlyList<LedgerEntry>> ReadBatchAsync(Guid? after, int batchSize, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LedgerEntry>>(Entries
            .OrderBy(e => e.Id)
            .SkipWhile(e => after is not null && e.Id.CompareTo(after.Value) <= 0)
            .Take(batchSize)
            .ToList());

    public Task RequeueAsync(EntryRecorded recorded, CancellationToken cancellationToken)
    {
        Requeued.Add(recorded);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryQueryCache : IQueryCache
{
    private readonly Dictionary<(Guid, string, string), object> entries = [];
    private readonly Dictionary<Guid, int> versions = [];

    public List<Guid> Invalidated { get; } = [];

    public int Reads { get; private set; }

    public Task<CacheLookup<T>> GetAsync<T>(Guid merchantId, string key, CancellationToken cancellationToken)
        where T : class
    {
        Reads++;
        var version = versions.GetValueOrDefault(merchantId).ToString();
        return Task.FromResult(new CacheLookup<T>(entries.GetValueOrDefault((merchantId, version, key)) as T, version));
    }

    public Task SetAsync<T>(Guid merchantId, string key, string version, T value, CancellationToken cancellationToken)
        where T : class
    {
        entries[(merchantId, version, key)] = value;
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(Guid merchantId, CancellationToken cancellationToken)
    {
        Invalidated.Add(merchantId);
        versions[merchantId] = versions.GetValueOrDefault(merchantId) + 1;
        return Task.CompletedTask;
    }
}

internal sealed class BrokenQueryCache : IQueryCache
{
    public Task<CacheLookup<T>> GetAsync<T>(Guid merchantId, string key, CancellationToken cancellationToken)
        where T : class => throw new InvalidOperationException("Cache unavailable");

    public Task SetAsync<T>(Guid merchantId, string key, string version, T value, CancellationToken cancellationToken)
        where T : class => throw new InvalidOperationException("Cache unavailable");

    public Task InvalidateAsync(Guid merchantId, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Cache unavailable");
}

internal sealed class WriteFailingQueryCache : IQueryCache
{
    public Task<CacheLookup<T>> GetAsync<T>(Guid merchantId, string key, CancellationToken cancellationToken)
        where T : class => Task.FromResult(new CacheLookup<T>(null, "0"));

    public Task SetAsync<T>(Guid merchantId, string key, string version, T value, CancellationToken cancellationToken)
        where T : class => throw new InvalidOperationException("Cache unavailable");

    public Task InvalidateAsync(Guid merchantId, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class CancelledQueryCache : IQueryCache
{
    public Task<CacheLookup<T>> GetAsync<T>(Guid merchantId, string key, CancellationToken cancellationToken)
        where T : class => throw new OperationCanceledException();

    public Task SetAsync<T>(Guid merchantId, string key, string version, T value, CancellationToken cancellationToken)
        where T : class => throw new OperationCanceledException();

    public Task InvalidateAsync(Guid merchantId, CancellationToken cancellationToken) =>
        throw new OperationCanceledException();
}

internal sealed class RecordingQueryCache : IQueryCache
{
    public List<string> Keys { get; } = [];

    public Task<CacheLookup<T>> GetAsync<T>(Guid merchantId, string key, CancellationToken cancellationToken)
        where T : class
    {
        Keys.Add(key);
        return Task.FromResult(new CacheLookup<T>(null, "0"));
    }

    public Task SetAsync<T>(Guid merchantId, string key, string version, T value, CancellationToken cancellationToken)
        where T : class => Task.CompletedTask;

    public Task InvalidateAsync(Guid merchantId, CancellationToken cancellationToken) => Task.CompletedTask;
}
