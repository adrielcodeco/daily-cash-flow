// @anchors
//   code: RPECD
//   ref: RPENT
//   updated_at: 2026-11-05
namespace Ledger.Core;

public sealed class RepublishEntries(IEntryStore store)
{
    public async Task<int> ExecuteAsync(int batchSize, CancellationToken cancellationToken)
    {
        var total = 0;
        Guid? after = null;
        IReadOnlyList<LedgerEntry> batch;

        do
        {
            batch = await store.ReadBatchAsync(after, batchSize, cancellationToken);
            foreach (var entry in batch)
            {
                await store.RequeueAsync(EntryRecorded.From(entry), cancellationToken);
            }

            total += batch.Count;
            after = batch.Count > 0 ? batch[^1].Id : after;
        }
        while (batch.Count == batchSize);

        return total;
    }
}
