// @anchors
//   code: LEDTS
//   ref: LEDEN
//   updated_at: 2026-11-05
//   layer: test
// Refs:
//   feature : services/ledger/src/Ledger.Core/LedgerEntry.feature
//   spec    : services/ledger/src/Ledger.Core/LedgerEntry.spec.md
using Ledger.Core;

namespace Ledger.Tests;

public sealed class LedgerEntryTests
{
    private static readonly Guid Merchant = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 11, 5);

    private static TimeProvider At(string instant) => new FixedClock(DateTimeOffset.Parse(instant));

    private static string RefusalCode(Action create) => Assert.Throws<DomainRuleException>(create).Code;

    [Fact(DisplayName = "LEDEN-R01 an entry without a merchant is refused")]
    public void RefusesMissingMerchant() =>
        Assert.Equal("merchant_required", RefusalCode(() =>
            LedgerEntry.Record(Guid.Empty, EntryType.Credit, 100, Today, null, At("2026-11-05T15:00:00Z"))));

    [Theory(DisplayName = "LEDEN-R02 an amount that is not positive is refused")]
    [InlineData(0)]
    [InlineData(-1)]
    public void RefusesInvalidAmount(long amount) =>
        Assert.Equal("invalid_amount", RefusalCode(() =>
            LedgerEntry.Record(Merchant, EntryType.Credit, amount, Today, null, At("2026-11-05T15:00:00Z"))));

    [Fact(DisplayName = "LEDEN-R03 the next day is refused")]
    public void RefusesFutureDate() =>
        Assert.Equal("future_accounting_date", RefusalCode(() =>
            LedgerEntry.Record(Merchant, EntryType.Credit, 100, Today.AddDays(1), null, At("2026-11-05T15:00:00Z"))));

    [Fact(DisplayName = "LEDEN-R03 the valid date follows the Sao Paulo time zone")]
    public void UsesBusinessTimeZone() =>
        Assert.Equal("future_accounting_date", RefusalCode(() =>
            LedgerEntry.Record(Merchant, EntryType.Credit, 100, Today.AddDays(1), null, At("2026-11-06T02:00:00Z"))));

    [Fact(DisplayName = "LEDEN-R04 a long description is refused")]
    public void RefusesLongDescription() =>
        Assert.Equal("description_too_long", RefusalCode(() =>
            LedgerEntry.Record(Merchant, EntryType.Credit, 100, Today, new string('x', 201), At("2026-11-05T15:00:00Z"))));

    [Fact(DisplayName = "LEDEN-R05 a valid entry is born with identity and creation time")]
    public void CreatesValidEntry()
    {
        var entry = LedgerEntry.Record(Merchant, EntryType.Credit, 35000, Today, "Sale", At("2026-11-05T15:00:00Z"));

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(DateTimeOffset.Parse("2026-11-05T15:00:00Z"), entry.CreatedAt);
    }
}

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
