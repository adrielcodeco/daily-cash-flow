// @anchors
//   code: GTBTS
//   ref: GTBAL
//   updated_at: 2026-11-09
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

    [Fact(DisplayName = "GTBAL-R08 the database shows only the merchant set in the transaction")]
    public async Task DatabaseIsolatesMerchants()
    {
        var merchant = Guid.NewGuid();
        await Credit(merchant, Day, 100);
        await Credit(Guid.NewGuid(), Day, 200);

        await using var api = NpgsqlDataSource.Create($"{database.GetConnectionString()};Options=-c role=consolidation_api");
        await using var connection = await api.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var withoutMerchant = new NpgsqlCommand("SELECT count(*) FROM daily_balance", connection, transaction);
        Assert.Equal(0L, await withoutMerchant.ExecuteScalarAsync());

        await using var setMerchant = new NpgsqlCommand($"SELECT set_config('app.merchant_id', '{merchant}', true)", connection, transaction);
        await setMerchant.ExecuteNonQueryAsync();
        await using var withMerchant = new NpgsqlCommand("SELECT count(*) FROM daily_balance", connection, transaction);
        Assert.Equal(1L, await withMerchant.ExecuteScalarAsync());
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

    [Fact(DisplayName = "APENT-R04 the consumer writes under row-level security, as the event's merchant")]
    public async Task ConsumerWritesUnderRowLevelSecurity()
    {
        await using var worker = NpgsqlDataSource.Create($"{database.GetConnectionString()};Options=-c role=consolidation_worker");
        var consumer = new PostgresBalanceStore(worker, TimeProvider.System);
        var merchant = Guid.NewGuid();

        await consumer.ApplyOnceAsync(new BalanceChange(Guid.NewGuid(), merchant, Day, 100, 0, AppliedAt), CancellationToken.None);

        Assert.Equal(100, (await Query(merchant, Day)).ClosingCents);
        await using var bypass = dataSource.CreateCommand("SELECT rolbypassrls FROM pg_roles WHERE rolname = 'consolidation_worker'");
        Assert.Equal(false, await bypass.ExecuteScalarAsync());
    }
}
