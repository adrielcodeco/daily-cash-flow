// @anchors
//   code: RPETS
//   ref: RPENT
//   updated_at: 2026-11-05
//   layer: test
// Refs:
//   feature : services/ledger/src/Ledger.Core/RepublishEntries.feature
//   spec    : services/ledger/src/Ledger.Core/RepublishEntries.spec.md
using Ledger.Core;

namespace Ledger.Tests;

public sealed class RepublishEntriesTests
{
    private static readonly DateOnly Today = new(2026, 11, 5);
    private static readonly FixedClock Clock = new(DateTimeOffset.Parse("2026-11-05T15:00:00Z"));

    private readonly InMemoryEntryStore store = new();

    private LedgerEntry Seed()
    {
        var entry = LedgerEntry.Record(Guid.NewGuid(), EntryType.Credit, 100, Today, null, Clock);
        store.Entries.Add(entry);
        return entry;
    }

    [Fact(DisplayName = "RPENT-R01 every entry goes back to pending")]
    public async Task RequeuesAllEntries()
    {
        Seed();
        Seed();
        Seed();

        var total = await new RepublishEntries(store).ExecuteAsync(2, CancellationToken.None);

        Assert.Equal(3, total);
        Assert.Equal(3, store.Requeued.Select(e => e.EventId).Distinct().Count());
    }

    [Fact(DisplayName = "RPENT-R02 a republished event keeps its identity")]
    public async Task KeepsEventIdentity()
    {
        var entry = Seed();

        await new RepublishEntries(store).ExecuteAsync(10, CancellationToken.None);

        Assert.Equal(entry.Id, Assert.Single(store.Requeued).EventId);
    }
}
