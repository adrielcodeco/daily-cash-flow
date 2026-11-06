// @anchors
//   code: DYBTS
//   ref: DYBAL
//   updated_at: 2026-11-06
//   layer: test
// Refs:
//   feature : services/consolidation/src/Consolidation.Core/DailyBalance.feature
//   spec    : services/consolidation/src/Consolidation.Core/DailyBalance.spec.md
using Consolidation.Core;

namespace Consolidation.Tests;

public sealed class DailyBalanceTests
{
    private static readonly DateOnly Day = new(2026, 11, 5);

    [Fact(DisplayName = "DYBAL-R01 closing adds credits and subtracts debits")]
    public void ComputesClosing() =>
        Assert.Equal(123000, new DailyBalance(Day, 100000, 35000, 12000, null).ClosingCents);

    [Fact(DisplayName = "DYBAL-R02 a day without movement repeats the opening")]
    public void KeepsOpeningWithoutMovement() =>
        Assert.Equal(100000, new DailyBalance(Day, 100000, 0, 0, null).ClosingCents);

    [Fact(DisplayName = "DYBAL-R03 an overflow in the closing raises an error")]
    public void RaisesOnOverflow() =>
        Assert.Throws<OverflowException>(() => new DailyBalance(Day, long.MaxValue, 1, 0, null).ClosingCents);

    [Fact(DisplayName = "DYBAL-R03 an underflow in the closing raises an error")]
    public void RaisesOnUnderflow() =>
        Assert.Throws<OverflowException>(() => new DailyBalance(Day, long.MinValue, 0, 1, null).ClosingCents);
}
