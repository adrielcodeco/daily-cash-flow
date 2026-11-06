// @anchors
//   code: IESTR
//   ref: RCENT
//   updated_at: 2026-11-05
namespace Ledger.Core;

public interface IEntryStore
{
    /// <summary>Persists the entry and its event atomically.</summary>
    Task AddAsync(LedgerEntry entry, EntryRecorded recorded, CancellationToken cancellationToken);

    Task<LedgerEntry?> FindAsync(Guid merchantId, Guid id, CancellationToken cancellationToken);
}
