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
        new GetDailyBalance(store).ExecuteAsync(merchant, date, CancellationToken.None);

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
}
