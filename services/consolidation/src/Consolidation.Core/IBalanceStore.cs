// @anchors
//   code: IBSTR
//   ref: APENT
//   updated_at: 2026-11-05
namespace Consolidation.Core;

public sealed record BalanceChange(
    Guid EventId,
    Guid MerchantId,
    DateOnly Date,
    long CreditsCents,
    long DebitsCents,
    DateTimeOffset OccurredAt);

public sealed record DayTotals(DateOnly Date, long CreditsCents, long DebitsCents);

public sealed record PeriodTotals(long OpeningCents, IReadOnlyList<DayTotals> Days, DateTimeOffset? UpdatedThrough);

public interface IBalanceStore
{
    /// <summary>Applies the change once per event. Returns false when the event was already applied.</summary>
    Task<bool> ApplyOnceAsync(BalanceChange change, CancellationToken cancellationToken);

    Task<DailyBalance> GetDailyAsync(Guid merchantId, DateOnly date, CancellationToken cancellationToken);

    Task<PeriodTotals> GetPeriodAsync(Guid merchantId, DateOnly start, DateOnly end, CancellationToken cancellationToken);

    Task ResetAsync(CancellationToken cancellationToken);
}

public interface IBalanceCache
{
    Task<DailyBalance?> GetAsync(Guid merchantId, DateOnly date, CancellationToken cancellationToken);

    Task SetAsync(Guid merchantId, DailyBalance balance, CancellationToken cancellationToken);

    /// <summary>Makes every cached balance of the merchant unreachable.</summary>
    Task InvalidateAsync(Guid merchantId, CancellationToken cancellationToken);
}
