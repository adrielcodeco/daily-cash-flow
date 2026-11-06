// @anchors
//   code: APECD
//   ref: APENT
//   updated_at: 2026-11-05
namespace Consolidation.Core;

public sealed class ApplyEntryRecorded(IBalanceStore store)
{
    public Task<bool> ExecuteAsync(EntryRecorded recorded, CancellationToken cancellationToken)
    {
        var credits = recorded.EntryType == EntryType.Credit ? recorded.AmountCents : 0;
        var debits = recorded.EntryType == EntryType.Debit ? recorded.AmountCents : 0;

        return store.ApplyOnceAsync(
            new BalanceChange(recorded.EventId, recorded.MerchantId, recorded.AccountingDate, credits, debits, recorded.OccurredAt),
            cancellationToken);
    }
}
