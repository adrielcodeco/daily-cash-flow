// @anchors
//   code: APECD
//   ref: APENT
//   updated_at: 2026-11-05
namespace Consolidation.Core;

public sealed class ApplyEntryRecorded(IBalanceStore store, IBalanceCache cache)
{
    public async Task<bool> ExecuteAsync(EntryRecorded recorded, CancellationToken cancellationToken)
    {
        var credits = recorded.EntryType == EntryType.Credit ? recorded.AmountCents : 0;
        var debits = recorded.EntryType == EntryType.Debit ? recorded.AmountCents : 0;

        var applied = await store.ApplyOnceAsync(
            new BalanceChange(recorded.EventId, recorded.MerchantId, recorded.AccountingDate, credits, debits, recorded.OccurredAt),
            cancellationToken);

        if (applied)
        {
            await InvalidateCacheAsync(recorded.MerchantId, cancellationToken);
        }

        return applied;
    }

    private async Task InvalidateCacheAsync(Guid merchantId, CancellationToken cancellationToken)
    {
        try
        {
            await cache.InvalidateAsync(merchantId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Cached balances expire on their own, so a cache failure must not fail the event.
        }
    }
}
