// @anchors
//   code: CESTS
//   ref: CESTR
//   updated_at: 2026-11-09
//   layer: test
// Refs:
//   feature : services/ledger/src/Ledger.Core/CachedEntryStore.feature
//   spec    : services/ledger/src/Ledger.Core/CachedEntryStore.spec.md
using Ledger.Core;

namespace Ledger.Tests;

public sealed class CachedEntryStoreTests
{
    private static readonly DateOnly Today = new(2026, 11, 5);
    private static readonly Guid Merchant = Guid.NewGuid();
    private static readonly CancellationToken None = CancellationToken.None;
    private static readonly FixedClock Clock = new(DateTimeOffset.Parse("2026-11-05T15:00:00Z"));

    private readonly InMemoryEntryStore inner = new();
    private readonly InMemoryQueryCache cache = new();

    private CachedEntryStore Store(string? targets, IQueryCache? usedCache = null) => new(inner, usedCache ?? cache, CachePolicy.Parse(targets));

    private static Task<AddOutcome> Add(IEntryStore store, LedgerEntry entry, string key = "request-0001") =>
        store.AddAsync(entry, EntryRecorded.From(entry), new IdempotencyClaim(key, "hash"), None);

    private static LedgerEntry NewEntry() => LedgerEntry.Record(Merchant, EntryType.Credit, 100, Today, null, Clock);

    [Theory(DisplayName = "CESTR-R01 without a target the query does not touch the cache")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown.target")]
    public async Task SkipsCacheByDefault(string? targets)
    {
        var store = Store(targets);

        await store.FindAsync(Merchant, Guid.NewGuid(), None);
        await store.ListAsync(Merchant, Today, null, 10, None);

        Assert.Equal(0, cache.Reads);
    }

    [Fact(DisplayName = "CESTR-R02 a route target enables only that route")]
    public async Task EnablesOneRoute()
    {
        var store = Store("entries.get");

        await store.ListAsync(Merchant, Today, null, 10, None);
        Assert.Equal(0, cache.Reads);

        await store.FindAsync(Merchant, Guid.NewGuid(), None);
        Assert.Equal(1, cache.Reads);
    }

    [Fact(DisplayName = "CESTR-R02 the list route has its own target")]
    public async Task EnablesListRoute()
    {
        var store = Store("entries.list");

        await store.FindAsync(Merchant, Guid.NewGuid(), None);
        Assert.Equal(0, cache.Reads);

        await store.ListAsync(Merchant, Today, null, 10, None);
        Assert.Equal(1, cache.Reads);
    }

    [Fact(DisplayName = "CESTR-R03 a table target enables every route of the table")]
    public async Task EnablesTable()
    {
        var entry = NewEntry();
        await Add(inner, entry);
        var store = Store("ledger_entry");

        await store.FindAsync(Merchant, entry.Id, None);
        await store.FindAsync(Merchant, entry.Id, None);
        await store.ListAsync(Merchant, Today, null, 10, None);
        await store.ListAsync(Merchant, Today, null, 10, None);

        Assert.Equal(1, inner.Finds);
        Assert.Equal(1, inner.Lists);
    }

    [Fact(DisplayName = "CESTR-R04 a cached value skips the store")]
    public async Task ReadsFromCache()
    {
        var entry = NewEntry();
        await cache.SetAsync(Merchant, $"entry:{entry.Id}", "0", entry, None);

        Assert.Equal(entry, await Store("ledger_entry").FindAsync(Merchant, entry.Id, None));
        Assert.Equal(0, inner.Finds);
    }

    [Fact(DisplayName = "CESTR-R04 a missing entry is not cached")]
    public async Task DoesNotCacheMissingEntry()
    {
        var store = Store("ledger_entry");
        var id = Guid.NewGuid();

        await store.FindAsync(Merchant, id, None);
        await store.FindAsync(Merchant, id, None);

        Assert.Equal(2, inner.Finds);
    }

    [Fact(DisplayName = "CESTR-R05 the query works with the cache down")]
    public async Task SurvivesReadFailure()
    {
        await Store("ledger_entry", new BrokenQueryCache()).ListAsync(Merchant, Today, null, 10, None);

        Assert.Equal(1, inner.Lists);
    }

    [Fact(DisplayName = "CESTR-R06 a recorded entry invalidates the merchant's cache")]
    public async Task InvalidatesOnWrite()
    {
        await Add(Store("ledger_entry"), NewEntry());

        Assert.Equal(Merchant, Assert.Single(cache.Invalidated));
    }

    [Fact(DisplayName = "CESTR-R06 a refused write does not invalidate the cache")]
    public async Task IgnoresRefusedWrite()
    {
        var original = NewEntry();
        var store = Store("ledger_entry");
        await Add(store, LedgerEntry.Reverse(original, Clock));

        var outcome = await Add(store, LedgerEntry.Reverse(original, Clock), "request-0002");

        Assert.Equal(AddOutcome.AlreadyReversed, outcome);
        Assert.Single(cache.Invalidated);
    }

    [Fact(DisplayName = "CESTR-R07 a cache failure does not stop the write")]
    public async Task SurvivesInvalidationFailure()
    {
        var outcome = await Add(Store("ledger_entry", new BrokenQueryCache()), NewEntry());

        Assert.Equal(AddOutcome.Added, outcome);
    }

    [Fact(DisplayName = "CESTR-R08 an invalidation during the query does not keep an old value")]
    public async Task KeepsVersionReadBeforeQuery()
    {
        var entry = NewEntry();
        await Add(inner, entry);
        var store = Store("ledger_entry");
        inner.DuringQuery = () =>
        {
            inner.DuringQuery = null;
            cache.InvalidateAsync(Merchant, None).GetAwaiter().GetResult();
        };

        await store.FindAsync(Merchant, entry.Id, None);
        await store.FindAsync(Merchant, entry.Id, None);

        Assert.Equal(2, inner.Finds);
    }

    [Theory(DisplayName = "CESTR-R02 one route target is enough to turn the cache on")]
    [InlineData("entries.get")]
    [InlineData("entries.list")]
    public void TurnsOnForOneRoute(string targets) =>
        Assert.True(CachedEntryStore.IsOn(CachePolicy.Parse(targets)));

    [Theory(DisplayName = "CESTR-R01 blank targets and targets of another service keep the cache off")]
    [InlineData(null)]
    [InlineData(" , ,")]
    [InlineData("daily_balance,balances.daily")]
    public void TreatsForeignTargetsAsOff(string? targets) =>
        Assert.False(CachedEntryStore.IsOn(CachePolicy.Parse(targets)));

    [Theory(DisplayName = "CESTR-R01 without a target the write does not touch the cache")]
    [InlineData(null)]
    [InlineData("daily_balance")]
    public async Task SkipsInvalidationByDefault(string? targets)
    {
        await Add(Store(targets), NewEntry());

        Assert.Empty(cache.Invalidated);
    }

    [Fact(DisplayName = "CESTR-R03 targets are trimmed and blanks are ignored")]
    public void ParsesTargets()
    {
        var policy = CachePolicy.Parse(" entries.get , ,ledger_entry ");

        Assert.True(policy.Covers("entries.get", "other"));
        Assert.True(policy.Covers("other", "ledger_entry"));
        Assert.False(policy.Covers("other", "another"));
    }

    [Fact(DisplayName = "CESTR-R04 different pages do not share cached values")]
    public async Task SeparatesPages()
    {
        var store = Store("ledger_entry");
        var cursor = Guid.NewGuid();

        await store.ListAsync(Merchant, Today, null, 10, None);
        await store.ListAsync(Merchant, Today, null, 20, None);
        await store.ListAsync(Merchant, Today, cursor, 10, None);
        await store.ListAsync(Merchant, Today.AddDays(-1), null, 10, None);

        Assert.Equal(4, inner.Lists);
    }

    [Fact(DisplayName = "CESTR-R04 entry and list queries do not share keys")]
    public async Task SeparatesRoutes()
    {
        var keys = new RecordingQueryCache();
        var store = new CachedEntryStore(inner, keys, CachePolicy.Parse("ledger_entry"));
        var id = Guid.NewGuid();

        await store.FindAsync(Merchant, id, None);
        await store.ListAsync(Merchant, Today, null, 10, None);

        Assert.Equal([$"entry:{id}", $"list:{Today:O}::10"], keys.Keys);
    }

    [Fact(DisplayName = "CESTR-R05 a failed cache write does not stop the query")]
    public async Task SurvivesWriteFailure()
    {
        var page = await Store("ledger_entry", new WriteFailingQueryCache()).ListAsync(Merchant, Today, null, 10, None);

        Assert.Empty(page.Items);
    }

    [Fact(DisplayName = "CESTR-R05 a cancelled read stops the query")]
    public async Task PropagatesCancelledRead() =>
        await Assert.ThrowsAsync<OperationCanceledException>(() => Store("ledger_entry", new CancelledQueryCache()).ListAsync(Merchant, Today, null, 10, None));

    [Fact(DisplayName = "CESTR-R07 a cancelled invalidation stops the write")]
    public async Task PropagatesCancelledInvalidation() =>
        await Assert.ThrowsAsync<OperationCanceledException>(() => Add(Store("ledger_entry", new CancelledQueryCache()), NewEntry()));

    [Fact(DisplayName = "CESTR-R01 the other operations reach the store")]
    public async Task ForwardsOtherOperations()
    {
        var entry = NewEntry();
        var store = Store("ledger_entry");
        await Add(store, entry);

        Assert.NotNull(await store.FindClaimAsync(Merchant, "request-0001", None));
        Assert.Single(await store.ReadBatchAsync(null, 10, None));

        await store.RequeueAsync([EntryRecorded.From(entry)], None);
        Assert.Single(inner.Requeued);
    }
}
