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

    [Fact(DisplayName = "LEDEN-R04 a description with exactly 200 characters is accepted")]
    public void AcceptsDescriptionAtLimit()
    {
        var entry = LedgerEntry.Record(Merchant, EntryType.Credit, 100, Today, new string('x', 200), At("2026-11-05T15:00:00Z"));

        Assert.Equal(200, entry.Description?.Length);
    }

    [Fact(DisplayName = "LEDEN-R05 a valid entry is born with identity and creation time")]
    public void CreatesValidEntry()
    {
        var entry = LedgerEntry.Record(Merchant, EntryType.Credit, 35000, Today, "Sale", At("2026-11-05T15:00:00Z"));

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(DateTimeOffset.Parse("2026-11-05T15:00:00Z"), entry.CreatedAt);
    }

    [Fact(DisplayName = "LEDEN-R06 a reversal inverts the type and keeps amount and date")]
    public void ReversesEntry()
    {
        var clock = At("2026-11-05T15:00:00Z");
        var original = LedgerEntry.Record(Merchant, EntryType.Credit, 35000, Today, null, clock);

        var reversal = LedgerEntry.Reverse(original, clock);

        Assert.Equal(EntryType.Debit, reversal.EntryType);
        Assert.Equal(35000, reversal.AmountCents);
        Assert.Equal(Today, reversal.AccountingDate);
        Assert.Equal(original.Id, reversal.ReversalOf);
    }

    [Fact(DisplayName = "LEDEN-R06 reversing a debit produces a credit")]
    public void ReversesDebit()
    {
        var clock = At("2026-11-05T15:00:00Z");
        var original = LedgerEntry.Record(Merchant, EntryType.Debit, 12000, Today, null, clock);

        Assert.Equal(EntryType.Credit, LedgerEntry.Reverse(original, clock).EntryType);
    }

    [Fact(DisplayName = "LEDEN-R07 a reversal cannot be reversed")]
    public void RefusesReversalOfReversal()
    {
        var clock = At("2026-11-05T15:00:00Z");
        var reversal = LedgerEntry.Reverse(LedgerEntry.Record(Merchant, EntryType.Credit, 100, Today, null, clock), clock);

        Assert.Equal("reversal_not_allowed", RefusalCode(() => LedgerEntry.Reverse(reversal, clock)));
    }
}
