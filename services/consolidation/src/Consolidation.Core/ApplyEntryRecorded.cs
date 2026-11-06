// @anchors
//   code: APECD
//   ref: APENT
//   updated_at: 2026-11-06
namespace Consolidation.Core;

public sealed class ApplyEntryRecorded(IBalanceStore store)
{
    public async Task<bool> ExecuteAsync(EntryRecorded recorded, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(recorded.EntryType))
        {
            throw new InvalidEventException($"Event {recorded.EventId} has an unknown entry type.");
        }

        var credits = recorded.EntryType == EntryType.Credit ? recorded.AmountCents : 0;
        var debits = recorded.EntryType == EntryType.Debit ? recorded.AmountCents : 0;

        return await store.ApplyOnceAsync(
            new BalanceChange(recorded.EventId, recorded.MerchantId, recorded.AccountingDate, credits, debits, recorded.OccurredAt),
            cancellationToken);
    }
}
