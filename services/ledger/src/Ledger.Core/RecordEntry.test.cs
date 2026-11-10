// @anchors
//   code: RCETS
//   ref: RCENT
//   updated_at: 2026-11-09
//   layer: test
// Refs:
//   feature : services/ledger/src/Ledger.Core/RecordEntry.feature
//   spec    : services/ledger/src/Ledger.Core/RecordEntry.spec.md
using Ledger.Core;

namespace Ledger.Tests;

public sealed class RecordEntryTests
{
    private const string Key = "request-0001";
    private static readonly Guid Merchant = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 11, 5);
    private static readonly FixedClock Clock = new(DateTimeOffset.Parse("2026-11-05T15:00:00Z"));

    private readonly InMemoryEntryStore store = new();
    private readonly RecordEntry useCase;

    public RecordEntryTests() => useCase = new RecordEntry(store, Clock);

    private static RecordEntryCommand Credit(long amount) => new(Merchant, EntryType.Credit, amount, Today, null);

    private Task<LedgerEntry> Record(long amount) => useCase.ExecuteAsync(Credit(amount), Key, CancellationToken.None);

    [Fact(DisplayName = "RCENT-R01 a valid entry reaches the store with its event")]
    public async Task StoresEntryAndEvent()
    {
        var entry = await Record(35000);

        Assert.Equal(entry, Assert.Single(store.Entries));
        Assert.Equal(entry.Id, Assert.Single(store.Events).EntryId);
    }

    [Fact(DisplayName = "RCENT-R01 an invalid entry never reaches the store")]
    public async Task DoesNotStoreInvalidEntry()
    {
        await Assert.ThrowsAsync<DomainRuleException>(() => Record(0));

        Assert.Empty(store.Entries);
    }

    [Fact(DisplayName = "RCENT-R02 recording uses only the store")]
    public void DependsOnlyOnTheStore()
    {
        var parameters = typeof(RecordEntry).GetConstructors().Single().GetParameters().Select(p => p.ParameterType);

        Assert.Equal([typeof(IEntryStore), typeof(TimeProvider)], parameters);
    }

    [Fact(DisplayName = "RCENT-R03 the event uses the entry identity and repeats its data")]
    public async Task EventCarriesEntryData()
    {
        var entry = await Record(35000);

        var recorded = Assert.Single(store.Events);
        Assert.Equal(entry.Id, recorded.EventId);
        Assert.Equal(EntryRecorded.EventType, recorded.Type);
        Assert.Equal(Merchant, recorded.MerchantId);
        Assert.Equal(35000, recorded.AmountCents);
        Assert.Equal(Today, recorded.AccountingDate);
    }

    [Fact(DisplayName = "RCENT-R04 a repeated key returns the original entry")]
    public async Task ReplaysRepeatedKey()
    {
        var first = await Record(35000);

        var second = await Record(35000);

        Assert.Equal(first.Id, second.Id);
        Assert.Single(store.Entries);
    }

    [Fact(DisplayName = "RCENT-R05 a key reused with other content is refused")]
    public async Task RefusesReusedKey()
    {
        await Record(35000);

        var refusal = await Assert.ThrowsAsync<DomainRuleException>(() => Record(1));

        Assert.Equal("idempotency_key_reused", refusal.Code);
        Assert.Equal(RuleKind.Conflict, refusal.Kind);
    }

    [Fact(DisplayName = "RCENT-R05 an empty description is other content than no description")]
    public async Task SeparatesEmptyDescription()
    {
        await Record(35000);

        var refusal = await Assert.ThrowsAsync<DomainRuleException>(() =>
            useCase.ExecuteAsync(Credit(35000) with { Description = "" }, Key, CancellationToken.None));

        Assert.Equal("idempotency_key_reused", refusal.Code);
    }

    [Fact(DisplayName = "RCENT-R06 a concurrent call receives the winning entry")]
    public async Task ReturnsConcurrentWinner()
    {
        var winner = LedgerEntry.Record(Merchant, EntryType.Credit, 35000, Today, null, Clock);
        var hash = RecordEntry.HashOf(Credit(35000));
        store.RacingWinner = (Key, new ClaimedEntry(hash, winner.Id), winner);

        var entry = await Record(35000);

        Assert.Equal(winner.Id, entry.Id);
    }
}
