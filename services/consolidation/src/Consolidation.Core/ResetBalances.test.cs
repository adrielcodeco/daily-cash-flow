// @anchors
//   code: RSBTS
//   ref: RSBAL
//   updated_at: 2026-11-05
//   layer: test
// Refs:
//   feature : services/consolidation/src/Consolidation.Core/ResetBalances.feature
//   spec    : services/consolidation/src/Consolidation.Core/ResetBalances.spec.md
using Consolidation.Core;
using Consolidation.Infrastructure;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Consolidation.Tests;

public sealed class ResetBalancesTests : IAsyncLifetime
{
    private static readonly DateOnly Day = new(2026, 11, 5);
    private static readonly Guid Merchant = Guid.NewGuid();

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

    private static BalanceChange Change(Guid eventId) => new(eventId, Merchant, Day, 35000, 0, DateTimeOffset.UtcNow);

    [Fact(DisplayName = "RSBAL-R01 a reset removes the balance")]
    public async Task ClearsBalance()
    {
        await store.ApplyOnceAsync(Change(Guid.NewGuid()), CancellationToken.None);

        await new ResetBalances(store).ExecuteAsync(CancellationToken.None);

        var balance = await store.GetDailyAsync(Merchant, Day, CancellationToken.None);
        Assert.Equal(0, balance.ClosingCents);
    }

    [Fact(DisplayName = "RSBAL-R02 republished events restore the balance")]
    public async Task RestoresBalanceFromEvents()
    {
        var change = Change(Guid.NewGuid());
        await store.ApplyOnceAsync(change, CancellationToken.None);
        await new ResetBalances(store).ExecuteAsync(CancellationToken.None);

        var applied = await store.ApplyOnceAsync(change, CancellationToken.None);

        var balance = await store.GetDailyAsync(Merchant, Day, CancellationToken.None);
        Assert.True(applied);
        Assert.Equal(35000, balance.ClosingCents);
    }
}
