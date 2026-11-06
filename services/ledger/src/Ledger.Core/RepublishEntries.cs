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

        while (true)
        {
            var batch = await store.ReadBatchAsync(after, batchSize, cancellationToken);
            foreach (var entry in batch)
            {
                await store.RequeueAsync(EntryRecorded.From(entry), cancellationToken);
            }

            total += batch.Count;
            if (batch.Count < batchSize)
            {
                return total;
            }

            after = batch[^1].Id;
        }
    }
}
