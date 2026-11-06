// @anchors
//   code: GTBTS
//   ref: GTBAL
//   updated_at: 2026-11-05
//   layer: test
// Refs:
//   feature : services/consolidation/src/Consolidation.Core/GetDailyBalance.feature
//   spec    : services/consolidation/src/Consolidation.Core/GetDailyBalance.spec.md
using Consolidation.Core;
using Consolidation.Infrastructure;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Consolidation.Tests;

[Trait("Category", "Integration")]
public sealed class GetDailyBalanceTests : IAsyncLifetime
{
    private static readonly DateOnly Day = new(2026, 11, 5);
    private static readonly DateTimeOffset AppliedAt = DateTimeOffset.Parse("2026-11-05T14:32:00-03:00");

    private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private NpgsqlDataSource dataSource = null!;
    private PostgresBalanceStore store = null!;

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        DatabaseMigrator.Run(database.GetConnectionString());
        dataSource = NpgsqlDataSource.Create(database.GetConnectionString());
        store = new PostgresBalanceStore(dataSource, TimeProvider.System);
    }

    public async Task DisposeAsync()
    {
        await dataSource.DisposeAsync();
        await database.DisposeAsync();
    }

    private Task Credit(Guid merchant, DateOnly date, long cents) =>
        store.ApplyOnceAsync(new BalanceChange(Guid.NewGuid(), merchant, date, cents, 0, AppliedAt), CancellationToken.None);

    private Task<DailyBalance> Query(Guid merchant, DateOnly date) =>
        new GetDailyBalance(store, new InMemoryBalanceCache()).ExecuteAsync(merchant, date, CancellationToken.None);

    [Fact(DisplayName = "GTBAL-R01 the query does not show another merchant")]
    public async Task IsolatesMerchants()
    {
        await Credit(Guid.NewGuid(), Day, 35000);

        var balance = await Query(Guid.NewGuid(), Day);

        Assert.Equal(0, balance.ClosingCents);
    }

    [Fact(DisplayName = "GTBAL-R02 the opening accumulates previous days")]
    public async Task AccumulatesOpening()
    {
        var merchant = Guid.NewGuid();
        await Credit(merchant, Day.AddDays(-1), 100000);

        var balance = await Query(merchant, Day);

        Assert.Equal(100000, balance.OpeningCents);
    }

    [Fact(DisplayName = "GTBAL-R03 the answer reports the last applied event")]
    public async Task ReportsLastUpdate()
    {
        var merchant = Guid.NewGuid();
        await Credit(merchant, Day, 100);

        var balance = await Query(merchant, Day);

        Assert.Equal(AppliedAt, balance.UpdatedThrough);
    }

    [Fact(DisplayName = "GTBAL-R04 a merchant without movement gets a zero balance")]
    public async Task ReturnsZeroWithoutMovement()
    {
        var balance = await Query(Guid.NewGuid(), Day);

        Assert.Equal(0, balance.ClosingCents);
        Assert.Null(balance.UpdatedThrough);
    }

    [Fact(DisplayName = "GTBAL-R07 a backdated entry changes the next day's opening")]
    public async Task BackdatedEntryChangesOpening()
    {
        var merchant = Guid.NewGuid();
        await Credit(merchant, Day, 1000);
        var before = await Query(merchant, Day);

        await Credit(merchant, Day.AddDays(-1), 5000);

        var after = await Query(merchant, Day);
        Assert.Equal(before.OpeningCents + 5000, after.OpeningCents);
    }
}

public sealed class GetDailyBalanceCacheTests
{
    private static readonly DateOnly Day = new(2026, 11, 5);

    [Fact(DisplayName = "GTBAL-R05 a cached balance skips the database")]
    public async Task ReadsFromCache()
    {
        var merchant = Guid.NewGuid();
        var fakeStore = new InMemoryBalanceStore();
        var cache = new InMemoryBalanceCache();
        await cache.SetAsync(merchant, new DailyBalance(Day, 100, 0, 0, null), CancellationToken.None);

        var balance = await new GetDailyBalance(fakeStore, cache).ExecuteAsync(merchant, Day, CancellationToken.None);

        Assert.Equal(100, balance.OpeningCents);
        Assert.Equal(0, fakeStore.DailyQueries);
    }

    [Fact(DisplayName = "GTBAL-R05 a balance read from the database is cached")]
    public async Task StoresBalanceInCache()
    {
        var merchant = Guid.NewGuid();
        var fakeStore = new InMemoryBalanceStore();
        var useCase = new GetDailyBalance(fakeStore, new InMemoryBalanceCache());

        await useCase.ExecuteAsync(merchant, Day, CancellationToken.None);
        await useCase.ExecuteAsync(merchant, Day, CancellationToken.None);

        Assert.Equal(1, fakeStore.DailyQueries);
    }

    [Fact(DisplayName = "GTBAL-R06 the query works with the cache down")]
    public async Task SurvivesCacheFailure()
    {
        var fakeStore = new InMemoryBalanceStore();

        await new GetDailyBalance(fakeStore, new BrokenBalanceCache()).ExecuteAsync(Guid.NewGuid(), Day, CancellationToken.None);

        Assert.Equal(1, fakeStore.DailyQueries);
    }
}
