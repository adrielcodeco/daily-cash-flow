// @anchors
//   code: GTPCD
//   ref: GTPRD
//   updated_at: 2026-11-05
namespace Consolidation.Core;

public sealed class BalanceQueryException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed record PeriodBalance(IReadOnlyList<DailyBalance> Items, DateTimeOffset? UpdatedThrough);

public sealed class GetPeriodBalance(IBalanceStore store)
{
    public const int MaxDays = 366;

    public async Task<PeriodBalance> ExecuteAsync(Guid merchantId, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        if (start > end)
        {
            throw new BalanceQueryException("invalid_period", "The start date cannot be after the end date.");
        }

        if (end.DayNumber - start.DayNumber + 1 > MaxDays)
        {
            throw new BalanceQueryException("period_too_long", "The period cannot exceed 366 days.");
        }

        var totals = await store.GetPeriodAsync(merchantId, start, end, cancellationToken);
        var byDate = totals.Days.ToDictionary(day => day.Date);
        var items = new List<DailyBalance>();
        var opening = totals.OpeningCents;

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var day = byDate.GetValueOrDefault(date);
            var balance = new DailyBalance(date, opening, day?.CreditsCents ?? 0, day?.DebitsCents ?? 0, totals.UpdatedThrough);
            items.Add(balance);
            opening = balance.ClosingCents;
        }

        return new PeriodBalance(items, totals.UpdatedThrough);
    }
}
