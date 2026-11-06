// @anchors
//   code: APETS
//   ref: APENT
//   updated_at: 2026-11-06
//   layer: test
// Refs:
//   feature : services/consolidation/src/Consolidation.Core/ApplyEntryRecorded.feature
//   spec    : services/consolidation/src/Consolidation.Core/ApplyEntryRecorded.spec.md
using System.Text.Json;
using Consolidation.Core;

namespace Consolidation.Tests;

public sealed class ApplyEntryRecordedTests
{
    private static readonly DateOnly Day = new(2026, 11, 5);
    private static readonly Guid Merchant = Guid.NewGuid();

    private readonly InMemoryBalanceStore store = new();

    private static EntryRecorded Event(EntryType type, long amount, Guid? merchant = null) =>
        new(Guid.NewGuid(), DateTimeOffset.Parse("2026-11-05T14:32:00-03:00"), merchant ?? Merchant, type, amount, Day);

    private Task Apply(EntryRecorded recorded) => new ApplyEntryRecorded(store).ExecuteAsync(recorded, CancellationToken.None);

    [Theory(DisplayName = "APENT-R07 an event of unknown type is refused and not marked as applied")]
    [InlineData(0)]
    [InlineData(7)]
    public async Task RefusesUnknownType(int type)
    {
        var recorded = Event((EntryType)type, 500);

        await Assert.ThrowsAsync<InvalidEventException>(() => Apply(recorded));

        Assert.Equal(0, store.AppliedEvents);
    }

    [Fact(DisplayName = "APENT-R07 a type sent as a number is not read")]
    public void RefusesNumericType() =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<EntryType>("7"));

    [Fact(DisplayName = "APENT-R01 a credit adds to the day's credits")]
    public async Task AppliesCredit()
    {
        await Apply(Event(EntryType.Credit, 35000));

        Assert.Equal((35000, 0), store.Totals(Merchant, Day));
    }

    [Fact(DisplayName = "APENT-R01 a debit adds to the day's debits")]
    public async Task AppliesDebit()
    {
        await Apply(Event(EntryType.Debit, 12000));

        Assert.Equal((0, 12000), store.Totals(Merchant, Day));
    }

    [Fact(DisplayName = "APENT-R02 a repeated event is ignored")]
    public async Task IgnoresRepeatedEvent()
    {
        var recorded = Event(EntryType.Credit, 35000);

        await Apply(recorded);
        await Apply(recorded);

        Assert.Equal((35000, 0), store.Totals(Merchant, Day));
    }

    [Fact(DisplayName = "APENT-R03 arrival order does not change the balance")]
    public async Task IgnoresArrivalOrder()
    {
        var credit = Event(EntryType.Credit, 35000);
        var debit = Event(EntryType.Debit, 12000);
        var reversedStore = new InMemoryBalanceStore();

        await Apply(credit);
        await Apply(debit);
        var useCase = new ApplyEntryRecorded(reversedStore);
        await useCase.ExecuteAsync(debit, CancellationToken.None);
        await useCase.ExecuteAsync(credit, CancellationToken.None);

        Assert.Equal(store.Totals(Merchant, Day), reversedStore.Totals(Merchant, Day));
    }

    [Fact(DisplayName = "APENT-R04 the aggregate stays with the event's merchant")]
    public async Task KeepsMerchantsApart()
    {
        var other = Guid.NewGuid();

        await Apply(Event(EntryType.Credit, 100));
        await Apply(Event(EntryType.Credit, 200, other));

        Assert.Equal((100, 0), store.Totals(Merchant, Day));
        Assert.Equal((200, 0), store.Totals(other, Day));
    }
}
