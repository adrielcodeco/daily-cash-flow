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

public interface IBalanceStore
{
    /// <summary>Applies the change once per event. Returns false when the event was already applied.</summary>
    Task<bool> ApplyOnceAsync(BalanceChange change, CancellationToken cancellationToken);

    Task<DailyBalance> GetDailyAsync(Guid merchantId, DateOnly date, CancellationToken cancellationToken);
}
