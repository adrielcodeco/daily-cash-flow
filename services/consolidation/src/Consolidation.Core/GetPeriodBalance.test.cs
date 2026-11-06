// @anchors
//   code: GTPTS
//   ref: GTPRD
//   updated_at: 2026-11-05
//   layer: test
// Refs:
//   feature : services/consolidation/src/Consolidation.Core/GetPeriodBalance.feature
//   spec    : services/consolidation/src/Consolidation.Core/GetPeriodBalance.spec.md
using Consolidation.Core;

namespace Consolidation.Tests;

public sealed class GetPeriodBalanceTests
{
    private static readonly DateOnly Start = new(2026, 11, 1);
    private static readonly Guid Merchant = Guid.NewGuid();

    private readonly InMemoryBalanceStore store = new();

    private Task<PeriodBalance> Query(DateOnly start, DateOnly end) =>
        new GetPeriodBalance(store).ExecuteAsync(Merchant, start, end, CancellationToken.None);

    private Task Credit(DateOnly date, long cents) =>
        store.ApplyOnceAsync(new BalanceChange(Guid.NewGuid(), Merchant, date, cents, 0, DateTimeOffset.UnixEpoch), CancellationToken.None);

    [Fact(DisplayName = "GTPRD-R01 a start after the end is refused")]
    public async Task RefusesInvertedPeriod()
    {
        var refusal = await Assert.ThrowsAsync<BalanceQueryException>(() => Query(Start.AddDays(1), Start));

        Assert.Equal("invalid_period", refusal.Code);
    }

    [Fact(DisplayName = "GTPRD-R02 a long period is refused")]
    public async Task RefusesLongPeriod()
    {
        var refusal = await Assert.ThrowsAsync<BalanceQueryException>(() => Query(Start, Start.AddDays(366)));

        Assert.Equal("period_too_long", refusal.Code);
    }

    [Fact(DisplayName = "GTPRD-R02 a period of exactly 366 days is accepted")]
    public async Task AcceptsLongestPeriod()
    {
        var period = await Query(Start, Start.AddDays(365));

        Assert.Equal(366, period.Items.Count);
    }

    [Fact(DisplayName = "GTPRD-R03 a day without movement appears")]
    public async Task IncludesEmptyDays()
    {
        await Credit(Start, 100);

        var period = await Query(Start, Start.AddDays(2));

        Assert.Equal(3, period.Items.Count);
    }

    [Fact(DisplayName = "GTPRD-R04 a closing becomes the next opening")]
    public async Task CarriesClosingForward()
    {
        await Credit(Start, 100);

        var period = await Query(Start, Start.AddDays(1));

        Assert.Equal(100, period.Items[1].OpeningCents);
    }

    [Fact(DisplayName = "GTPRD-R04 a debit reduces the next opening")]
    public async Task CarriesDebitForward()
    {
        await store.ApplyOnceAsync(new BalanceChange(Guid.NewGuid(), Merchant, Start, 0, 40, DateTimeOffset.UnixEpoch), CancellationToken.None);

        var period = await Query(Start, Start.AddDays(1));

        Assert.Equal(-40, period.Items[1].OpeningCents);
    }

    [Fact(DisplayName = "GTPRD-R05 the query uses the requesting merchant")]
    public async Task UsesRequestingMerchant()
    {
        await Query(Start, Start);

        Assert.Equal(Merchant, store.PeriodMerchant);
    }
}
