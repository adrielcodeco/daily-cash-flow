// @anchors
//   code: LSTTS
//   ref: LSTEN
//   updated_at: 2026-11-05
//   layer: test
// Refs:
//   feature : services/ledger/src/Ledger.Core/ListEntries.feature
//   spec    : services/ledger/src/Ledger.Core/ListEntries.spec.md
using Ledger.Core;

namespace Ledger.Tests;

public sealed class ListEntriesTests
{
    private static readonly Guid Merchant = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 11, 5);
    private static readonly FixedClock Clock = new(DateTimeOffset.Parse("2026-11-05T15:00:00Z"));

    private readonly InMemoryEntryStore store = new();

    private void Seed(int count, Guid? merchant = null)
    {
        for (var i = 0; i < count; i++)
        {
            store.Entries.Add(LedgerEntry.Record(merchant ?? Merchant, EntryType.Credit, 100, Today, null, Clock));
        }
    }

    private Task<EntryPage> List(int limit) =>
        new ListEntries(store).ExecuteAsync(Merchant, Today, null, limit, CancellationToken.None);

    [Fact(DisplayName = "LSTEN-R01 the list uses the requested merchant and date")]
    public async Task FiltersByMerchantAndDate()
    {
        Seed(1);
        Seed(1, Guid.NewGuid());

        var page = await List(10);

        Assert.Equal(Merchant, Assert.Single(page.Items).MerchantId);
    }

    [Theory(DisplayName = "LSTEN-R02 a limit out of range is refused")]
    [InlineData(0)]
    [InlineData(201)]
    public async Task RefusesInvalidLimit(int limit)
    {
        var refusal = await Assert.ThrowsAsync<DomainRuleException>(() => List(limit));

        Assert.Equal("invalid_limit", refusal.Code);
    }

    [Theory(DisplayName = "LSTEN-R02 the limits of the range are accepted")]
    [InlineData(1)]
    [InlineData(200)]
    public async Task AcceptsLimitsOfTheRange(int limit)
    {
        Seed(1);

        var page = await List(limit);

        Assert.Single(page.Items);
    }

    [Fact(DisplayName = "LSTEN-R03 the next position appears when more entries exist")]
    public async Task ReportsNextPosition()
    {
        Seed(3);

        var page = await List(2);

        Assert.Equal(2, page.Items.Count);
        Assert.NotNull(page.NextCursor);
    }

    [Fact(DisplayName = "LSTEN-R03 the last page has no next position")]
    public async Task OmitsNextPositionOnLastPage()
    {
        Seed(2);

        var page = await List(2);

        Assert.Null(page.NextCursor);
    }
}
