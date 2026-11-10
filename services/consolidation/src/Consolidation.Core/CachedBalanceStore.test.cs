// @anchors
//   code: CBSTS
//   ref: CBSTR
//   updated_at: 2026-11-09
//   layer: test
// Refs:
//   feature : services/consolidation/src/Consolidation.Core/CachedBalanceStore.feature
//   spec    : services/consolidation/src/Consolidation.Core/CachedBalanceStore.spec.md
using Consolidation.Core;

namespace Consolidation.Tests;

public sealed class CachedBalanceStoreTests
{
    private static readonly DateOnly Day = new(2026, 11, 5);
    private static readonly Guid Merchant = Guid.NewGuid();
    private static readonly CancellationToken None = CancellationToken.None;

    private readonly InMemoryBalanceStore inner = new();
    private readonly InMemoryQueryCache cache = new();

    private CachedBalanceStore Store(string? targets, IQueryCache? usedCache = null) => new(inner, usedCache ?? cache, CachePolicy.Parse(targets));

    private static BalanceChange Change() => new(Guid.NewGuid(), Merchant, Day, 100, 0, DateTimeOffset.Parse("2026-11-05T15:00:00Z"));

    [Theory(DisplayName = "CBSTR-R01 without a target the query does not touch the cache")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown.target")]
    public async Task SkipsCacheByDefault(string? targets)
    {
        var store = Store(targets);

        await store.GetDailyAsync(Merchant, Day, None);
        await store.GetPeriodAsync(Merchant, Day, Day, None);

        Assert.Equal(0, cache.Reads);
    }

    [Fact(DisplayName = "CBSTR-R02 a route target enables only that route")]
    public async Task EnablesOneRoute()
    {
        var store = Store("balances.daily");

        await store.GetPeriodAsync(Merchant, Day, Day, None);
        Assert.Equal(0, cache.Reads);

        await store.GetDailyAsync(Merchant, Day, None);
        Assert.Equal(1, cache.Reads);
    }

    [Fact(DisplayName = "CBSTR-R02 the period route has its own target")]
    public async Task EnablesPeriodRoute()
    {
        var store = Store("balances.period");

        await store.GetDailyAsync(Merchant, Day, None);
        Assert.Equal(0, cache.Reads);

        await store.GetPeriodAsync(Merchant, Day, Day, None);
        Assert.Equal(1, cache.Reads);
    }

    [Fact(DisplayName = "CBSTR-R03 a table target enables every route of the table")]
    public async Task EnablesTable()
    {
        var store = Store(" DAILY_BALANCE ");

        await store.GetDailyAsync(Merchant, Day, None);
        await store.GetDailyAsync(Merchant, Day, None);
        await store.GetPeriodAsync(Merchant, Day, Day, None);
        await store.GetPeriodAsync(Merchant, Day, Day, None);

        Assert.Equal(1, inner.DailyQueries);
        Assert.Equal(1, inner.PeriodQueries);
    }

    [Fact(DisplayName = "CBSTR-R04 a cached value skips the store")]
    public async Task ReadsFromCache()
    {
        await cache.SetAsync(Merchant, $"daily:{Day:O}", "0.0", new DailyBalance(Day, 100, 0, 0, null), None);

        var balance = await Store("daily_balance").GetDailyAsync(Merchant, Day, None);

        Assert.Equal(100, balance.OpeningCents);
        Assert.Equal(0, inner.DailyQueries);
    }

    [Fact(DisplayName = "CBSTR-R04 merchants do not share cached values")]
    public async Task SeparatesMerchants()
    {
        var store = Store("daily_balance");

        await store.GetDailyAsync(Merchant, Day, None);
        await store.GetDailyAsync(Guid.NewGuid(), Day, None);

        Assert.Equal(2, inner.DailyQueries);
    }

    [Fact(DisplayName = "CBSTR-R05 the query works with the cache down")]
    public async Task SurvivesReadFailure()
    {
        await Store("daily_balance", new BrokenQueryCache()).GetDailyAsync(Merchant, Day, None);

        Assert.Equal(1, inner.DailyQueries);
    }

    [Fact(DisplayName = "CBSTR-R06 an applied event invalidates the merchant's cache")]
    public async Task InvalidatesOnEvent()
    {
        await Store("daily_balance").ApplyOnceAsync(Change(), None);

        Assert.Equal(Merchant, Assert.Single(cache.Invalidated));
    }

    [Fact(DisplayName = "CBSTR-R06 a repeated event does not invalidate the cache")]
    public async Task IgnoresRepeatedEvent()
    {
        var store = Store("daily_balance");
        var change = Change();

        await store.ApplyOnceAsync(change, None);
        var applied = await store.ApplyOnceAsync(change, None);

        Assert.False(applied);
        Assert.Single(cache.Invalidated);
    }

    [Fact(DisplayName = "CBSTR-R07 a cache failure does not stop the event")]
    public async Task SurvivesInvalidationFailure()
    {
        await Store("daily_balance", new BrokenQueryCache()).ApplyOnceAsync(Change(), None);

        Assert.Equal((100, 0), inner.Totals(Merchant, Day));
    }

    [Fact(DisplayName = "CBSTR-R08 an invalidation during the query does not keep an old value")]
    public async Task KeepsVersionReadBeforeQuery()
    {
        var store = Store("daily_balance");
        inner.DuringQuery = () =>
        {
            inner.DuringQuery = null;
            cache.InvalidateAsync(Merchant, None).GetAwaiter().GetResult();
        };

        await store.GetDailyAsync(Merchant, Day, None);
        await store.GetDailyAsync(Merchant, Day, None);

        Assert.Equal(2, inner.DailyQueries);
    }

    [Theory(DisplayName = "CBSTR-R02 one route target is enough to turn the cache on")]
    [InlineData("balances.daily")]
    [InlineData("balances.period")]
    public void TurnsOnForOneRoute(string targets) =>
        Assert.True(CachedBalanceStore.IsOn(CachePolicy.Parse(targets)));

    [Theory(DisplayName = "CBSTR-R01 blank targets and targets of another service keep the cache off")]
    [InlineData(null)]
    [InlineData(" , ,")]
    [InlineData("ledger_entry,entries.get")]
    public void TreatsForeignTargetsAsOff(string? targets) =>
        Assert.False(CachedBalanceStore.IsOn(CachePolicy.Parse(targets)));

    [Theory(DisplayName = "CBSTR-R01 without a target the event does not touch the cache")]
    [InlineData(null)]
    [InlineData("ledger_entry")]
    public async Task SkipsInvalidationByDefault(string? targets)
    {
        await Store(targets).ApplyOnceAsync(Change(), None);

        Assert.Empty(cache.Invalidated);
    }

    [Fact(DisplayName = "CBSTR-R03 targets are trimmed and blanks are ignored")]
    public void ParsesTargets()
    {
        var policy = CachePolicy.Parse(" balances.daily , ,daily_balance ");

        Assert.True(policy.Covers("balances.daily", "other"));
        Assert.True(policy.Covers("other", "daily_balance"));
        Assert.False(policy.Covers("other", "another"));
    }

    [Fact(DisplayName = "CBSTR-R04 different periods do not share cached values")]
    public async Task SeparatesPeriods()
    {
        var store = Store("daily_balance");

        await store.GetPeriodAsync(Merchant, Day, Day, None);
        await store.GetPeriodAsync(Merchant, Day, Day.AddDays(1), None);
        await store.GetPeriodAsync(Merchant, Day.AddDays(-1), Day, None);

        Assert.Equal(3, inner.PeriodQueries);
    }

    [Fact(DisplayName = "CBSTR-R04 daily and period queries do not share keys")]
    public async Task SeparatesRoutes()
    {
        var keys = new RecordingQueryCache();
        var store = new CachedBalanceStore(inner, keys, CachePolicy.Parse("daily_balance"));

        await store.GetDailyAsync(Merchant, Day, None);
        await store.GetPeriodAsync(Merchant, Day, Day, None);

        Assert.Equal([$"daily:{Day:O}", $"period:{Day:O}:{Day:O}"], keys.Keys);
    }

    [Fact(DisplayName = "CBSTR-R05 a failed cache write does not stop the query")]
    public async Task SurvivesWriteFailure()
    {
        var balance = await Store("daily_balance", new WriteFailingQueryCache()).GetDailyAsync(Merchant, Day, None);

        Assert.Equal(Day, balance.Date);
    }

    [Fact(DisplayName = "CBSTR-R05 a cancelled read stops the query")]
    public async Task PropagatesCancelledRead() =>
        await Assert.ThrowsAsync<OperationCanceledException>(() => Store("daily_balance", new CancelledQueryCache()).GetDailyAsync(Merchant, Day, None));

    [Fact(DisplayName = "CBSTR-R07 a cancelled invalidation stops the event")]
    public async Task PropagatesCancelledInvalidation() =>
        await Assert.ThrowsAsync<OperationCanceledException>(() => Store("daily_balance", new CancelledQueryCache()).ApplyOnceAsync(Change(), None));

    [Fact(DisplayName = "CBSTR-R01 the reset reaches the store")]
    public async Task ForwardsReset()
    {
        await Store(null).ResetAsync(None);

        Assert.Equal(1, inner.Resets);
    }

    [Fact(DisplayName = "CBSTR-R09 the reset discards the cached balances of every merchant")]
    public async Task DiscardsCacheOnReset()
    {
        var store = Store("daily_balance");
        await store.GetDailyAsync(Merchant, Day, None);

        await store.ResetAsync(None);
        await store.GetDailyAsync(Merchant, Day, None);

        Assert.Equal(2, inner.DailyQueries);
    }

    [Fact(DisplayName = "CBSTR-R09 a cache failure fails the reset")]
    public async Task FailsResetWithoutCache() =>
        await Assert.ThrowsAsync<InvalidOperationException>(() => Store("daily_balance", new BrokenQueryCache()).ResetAsync(None));
}
