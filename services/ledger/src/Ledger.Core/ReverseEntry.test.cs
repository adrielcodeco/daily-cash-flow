// @anchors
//   code: RVETS
//   ref: RVENT
//   updated_at: 2026-11-05
//   layer: test
// Refs:
//   feature : services/ledger/src/Ledger.Core/ReverseEntry.feature
//   spec    : services/ledger/src/Ledger.Core/ReverseEntry.spec.md
using Ledger.Core;

namespace Ledger.Tests;

public sealed class ReverseEntryTests
{
    private static readonly Guid Merchant = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 11, 5);
    private static readonly FixedClock Clock = new(DateTimeOffset.Parse("2026-11-05T15:00:00Z"));

    private readonly InMemoryEntryStore store = new();
    private readonly LedgerEntry original = LedgerEntry.Record(Merchant, EntryType.Credit, 35000, Today, null, Clock);
    private readonly ReverseEntry useCase;

    public ReverseEntryTests()
    {
        store.Entries.Add(original);
        useCase = new ReverseEntry(store, Clock);
    }

    private Task<LedgerEntry> Reverse(Guid merchant, string key) =>
        useCase.ExecuteAsync(merchant, original.Id, key, CancellationToken.None);

    [Fact(DisplayName = "RVENT-R01 another merchant's entry is not found")]
    public async Task HidesOtherMerchants()
    {
        var refusal = await Assert.ThrowsAsync<DomainRuleException>(() => Reverse(Guid.NewGuid(), "request-0001"));

        Assert.Equal("entry_not_found", refusal.Code);
        Assert.Equal(RuleKind.NotFound, refusal.Kind);
    }

    [Fact(DisplayName = "RVENT-R02 a second reversal is refused")]
    public async Task RefusesSecondReversal()
    {
        await Reverse(Merchant, "request-0001");

        var refusal = await Assert.ThrowsAsync<DomainRuleException>(() => Reverse(Merchant, "request-0002"));

        Assert.Equal("already_reversed", refusal.Code);
    }

    [Fact(DisplayName = "RVENT-R04 a key used for another entry is refused")]
    public async Task RefusesKeyOfAnotherEntry()
    {
        var other = LedgerEntry.Record(Merchant, EntryType.Credit, 100, Today, null, Clock);
        store.Entries.Add(other);
        await Reverse(Merchant, "request-0001");

        var refusal = await Assert.ThrowsAsync<DomainRuleException>(() =>
            useCase.ExecuteAsync(Merchant, other.Id, "request-0001", CancellationToken.None));

        Assert.Equal("idempotency_key_reused", refusal.Code);
    }

    [Fact(DisplayName = "RVENT-R03 the reversal is stored with its event")]
    public async Task StoresReversalAndEvent()
    {
        var reversal = await Reverse(Merchant, "request-0001");

        Assert.Contains(reversal, store.Entries);
        Assert.Equal(reversal.Id, Assert.Single(store.Events).EntryId);
    }

    [Fact(DisplayName = "RVENT-R04 a repeated key returns the original reversal")]
    public async Task ReplaysRepeatedKey()
    {
        var first = await Reverse(Merchant, "request-0001");

        var second = await Reverse(Merchant, "request-0001");

        Assert.Equal(first.Id, second.Id);
    }
}
