// @anchors
//   code: IBSTR
//   ref: APENT
//   updated_at: 2026-11-06
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

public sealed record CacheLookup<T>(T? Value, string Version)
    where T : class;

public interface IQueryCache
{
    /// <summary>Reads a value and the merchant's cache version, which a later write must reuse.</summary>
    Task<CacheLookup<T>> GetAsync<T>(Guid merchantId, string key, CancellationToken cancellationToken)
        where T : class;

    Task SetAsync<T>(Guid merchantId, string key, string version, T value, CancellationToken cancellationToken)
        where T : class;

    /// <summary>Makes every cached value of the merchant unreachable.</summary>
    Task InvalidateAsync(Guid merchantId, CancellationToken cancellationToken);
}
