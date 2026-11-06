// @anchors
//   code: ESITS
//   layer: test
//   updated_at: 2026-11-06
using Ledger.Core;
using Ledger.Infrastructure;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Ledger.Tests;

[Trait("Category", "Integration")]
public sealed class EntryStoreIntegrationTests : IAsyncLifetime
{
    private const int ConcurrentCalls = 8;
    private static readonly DateOnly Today = new(2026, 11, 5);
    private static readonly FixedClock Clock = new(DateTimeOffset.Parse("2026-11-05T15:00:00Z"));

    private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly Guid merchant = Guid.NewGuid();
    private NpgsqlDataSource dataSource = null!;
    private PostgresEntryStore store = null!;

    public async Task InitializeAsync()
    {
        await database.StartAsync();
        DatabaseMigrator.Run(database.GetConnectionString());
        dataSource = NpgsqlDataSource.Create(database.GetConnectionString());
        store = new PostgresEntryStore(dataSource);
    }

    public async Task DisposeAsync()
    {
        await dataSource.DisposeAsync();
        await database.DisposeAsync();
    }

    private async Task<long> CountAsync(string table)
    {
        await using var command = dataSource.CreateCommand($"SELECT count(*) FROM {table}");
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private Task<LedgerEntry> Record(string key, long amount = 100) => new RecordEntry(store, Clock).ExecuteAsync(
        new RecordEntryCommand(merchant, EntryType.Credit, amount, Today, "Sale"), key, CancellationToken.None);

    [Fact(DisplayName = "RCENT-R01 the entry, the event and the claim are stored together")]
    public async Task StoresEverythingAtomically()
    {
        await Record("request-0001");

        Assert.Equal(1, await CountAsync("ledger_entry"));
        Assert.Equal(1, await CountAsync("outbox"));
        Assert.Equal(1, await CountAsync("idempotency_key"));
    }

    [Fact(DisplayName = "LEDEN-R08 the database also refuses an amount above the limit")]
    public async Task DatabaseRefusesAmountAboveLimit()
    {
        var valid = LedgerEntry.Record(merchant, EntryType.Credit, 100, Today, null, Clock);
        var entry = valid with { AmountCents = LedgerEntry.MaxAmountCents + 1 };

        await Assert.ThrowsAsync<PostgresException>(() => store.AddAsync(
            entry, EntryRecorded.From(entry), new IdempotencyClaim("request-0001", "hash"), CancellationToken.None));

        Assert.Equal(0, await CountAsync("ledger_entry"));
        Assert.Equal(0, await CountAsync("outbox"));
    }

    [Fact(DisplayName = "LEDEN-R09 the store refuses an unknown type instead of guessing")]
    public async Task StoreRefusesUnknownType()
    {
        var valid = LedgerEntry.Record(merchant, EntryType.Credit, 100, Today, null, Clock);
        var entry = valid with { EntryType = (EntryType)7 };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => store.AddAsync(
            entry, EntryRecorded.From(entry), new IdempotencyClaim("request-0001", "hash"), CancellationToken.None));

        Assert.Equal(0, await CountAsync("ledger_entry"));
    }

    [Fact(DisplayName = "RCENT-R01 the API role can store an entry for its merchant")]
    public async Task ApiRoleStoresEntries()
    {
        await using var api = NpgsqlDataSource.Create($"{database.GetConnectionString()};Options=-c role=ledger_api");
        var apiStore = new PostgresEntryStore(api);
        var entry = await new RecordEntry(apiStore, Clock).ExecuteAsync(
            new RecordEntryCommand(merchant, EntryType.Credit, 100, Today, "Sale"), "request-0001", CancellationToken.None);

        Assert.NotNull(await apiStore.FindAsync(merchant, entry.Id, CancellationToken.None));
        Assert.Null(await apiStore.FindAsync(Guid.NewGuid(), entry.Id, CancellationToken.None));
        Assert.Single((await apiStore.ListAsync(merchant, Today, null, 10, CancellationToken.None)).Items);
    }

    [Fact(DisplayName = "RCENT-R01 a failed write leaves no event behind")]
    public async Task RollsBackOnConflict()
    {
        var first = await Record("request-0001");
        var duplicate = LedgerEntry.Record(merchant, EntryType.Credit, 100, Today, null, Clock);

        var outcome = await store.AddAsync(
            duplicate, EntryRecorded.From(duplicate), new IdempotencyClaim("request-0001", "hash"), CancellationToken.None);

        Assert.Equal(AddOutcome.KeyAlreadyClaimed, outcome);
        Assert.Equal(1, await CountAsync("ledger_entry"));
        Assert.Equal(1, await CountAsync("outbox"));
        Assert.NotNull(await store.FindAsync(merchant, first.Id, CancellationToken.None));
    }

    [Fact(DisplayName = "RCENT-R06 concurrent calls with one key create one entry")]
    public async Task HandlesConcurrentKey()
    {
        var entries = await Task.WhenAll(Enumerable.Range(0, ConcurrentCalls).Select(_ => Record("request-0001")));

        Assert.Single(entries.Select(entry => entry.Id).Distinct());
        Assert.Equal(1, await CountAsync("ledger_entry"));
    }

    [Fact(DisplayName = "RVENT-R02 concurrent reversals create one reversal")]
    public async Task HandlesConcurrentReversals()
    {
        var original = await Record("request-0001");
        var useCase = new ReverseEntry(store, Clock);

        var attempts = Enumerable.Range(0, ConcurrentCalls)
            .Select(i => useCase.ExecuteAsync(merchant, original.Id, $"reversal-{i:0000}", CancellationToken.None));
        var results = await Task.WhenAll(attempts.Select(async attempt =>
        {
            try
            {
                await attempt;
                return "reversed";
            }
            catch (DomainRuleException refusal)
            {
                return refusal.Code;
            }
        }));

        Assert.Single(results, "reversed");
        Assert.All(results.Where(r => r != "reversed"), code => Assert.Equal("already_reversed", code));
        Assert.Equal(2, await CountAsync("ledger_entry"));
    }

    [Fact(DisplayName = "LSTEN-R03 pages walk through every entry once")]
    public async Task WalksPages()
    {
        for (var i = 0; i < 5; i++)
        {
            await Record($"request-{i:0000}");
        }

        var seen = new List<Guid>();
        Guid? cursor = null;
        do
        {
            var page = await store.ListAsync(merchant, Today, cursor, 2, CancellationToken.None);
            seen.AddRange(page.Items.Select(entry => entry.Id));
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        Assert.Equal(5, seen.Distinct().Count());
    }

    [Fact(DisplayName = "RPENT-R01 republishing marks published events as pending")]
    public async Task RequeuesPublishedEvents()
    {
        await Record("request-0001");
        await using (var publish = dataSource.CreateCommand("UPDATE outbox SET published_at = now()"))
        {
            await publish.ExecuteNonQueryAsync();
        }

        await new RepublishEntries(store).ExecuteAsync(10, CancellationToken.None);

        await using var pending = dataSource.CreateCommand("SELECT count(*) FROM outbox WHERE published_at IS NULL");
        Assert.Equal(1L, await pending.ExecuteScalarAsync());
    }

    [Fact(DisplayName = "OBRLY-R01 the claimed batch skips rows locked by another publisher")]
    public async Task SkipsLockedRows()
    {
        await Record("request-0001");
        var outbox = new PostgresOutbox(dataSource, Clock);

        await using var first = await outbox.ClaimPendingAsync(10, CancellationToken.None);
        await using var second = await outbox.ClaimPendingAsync(10, CancellationToken.None);

        Assert.Single(first.Messages);
        Assert.Empty(second.Messages);
    }
}
