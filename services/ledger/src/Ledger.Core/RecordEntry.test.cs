// @anchors
//   code: RCETS
//   ref: RCENT
//   updated_at: 2026-11-05
//   layer: test
// Refs:
//   feature : services/ledger/src/Ledger.Core/RecordEntry.feature
//   spec    : services/ledger/src/Ledger.Core/RecordEntry.spec.md
using Ledger.Core;

namespace Ledger.Tests;

public sealed class RecordEntryTests
{
    private static readonly Guid Merchant = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 11, 5);

    private readonly SpyStore store = new();
    private readonly RecordEntry useCase;

    public RecordEntryTests() =>
        useCase = new RecordEntry(store, new FixedClock(DateTimeOffset.Parse("2026-11-05T15:00:00Z")));

    private static RecordEntryCommand Credit(long amount) => new(Merchant, EntryType.Credit, amount, Today, null);

    [Fact(DisplayName = "RCENT-R01 a valid entry reaches the store with its event")]
    public async Task StoresEntryAndEvent()
    {
        var entry = await useCase.ExecuteAsync(Credit(35000), CancellationToken.None);

        var (storedEntry, storedEvent) = Assert.Single(store.Added);
        Assert.Equal(entry, storedEntry);
        Assert.Equal(entry.Id, storedEvent.EntryId);
    }

    [Fact(DisplayName = "RCENT-R01 an invalid entry never reaches the store")]
    public async Task DoesNotStoreInvalidEntry()
    {
        await Assert.ThrowsAsync<DomainRuleException>(() => useCase.ExecuteAsync(Credit(0), CancellationToken.None));

        Assert.Empty(store.Added);
    }

    [Fact(DisplayName = "RCENT-R02 recording uses only the store")]
    public void DependsOnlyOnTheStore()
    {
        var parameters = typeof(RecordEntry).GetConstructors().Single().GetParameters().Select(p => p.ParameterType);

        Assert.Equal([typeof(IEntryStore), typeof(TimeProvider)], parameters);
    }

    [Fact(DisplayName = "RCENT-R03 the event repeats the entry data with its own identity")]
    public async Task EventCarriesEntryData()
    {
        var entry = await useCase.ExecuteAsync(Credit(35000), CancellationToken.None);

        var recorded = Assert.Single(store.Added).Recorded;
        Assert.NotEqual(entry.Id, recorded.EventId);
        Assert.Equal(EntryRecorded.EventType, recorded.Type);
        Assert.Equal(Merchant, recorded.MerchantId);
        Assert.Equal(35000, recorded.AmountCents);
        Assert.Equal(Today, recorded.AccountingDate);
    }

    private sealed class SpyStore : IEntryStore
    {
        public List<(LedgerEntry Entry, EntryRecorded Recorded)> Added { get; } = [];

        public Task AddAsync(LedgerEntry entry, EntryRecorded recorded, CancellationToken cancellationToken)
        {
            Added.Add((entry, recorded));
            return Task.CompletedTask;
        }

        public Task<LedgerEntry?> FindAsync(Guid merchantId, Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<LedgerEntry?>(null);
    }
}
