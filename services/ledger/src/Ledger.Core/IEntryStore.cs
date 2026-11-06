// @anchors
//   code: IESTR
//   ref: RCENT
//   updated_at: 2026-11-05
namespace Ledger.Core;

public sealed record IdempotencyClaim(string Key, string RequestHash);

public sealed record ClaimedEntry(string RequestHash, Guid EntryId);

public sealed record EntryPage(IReadOnlyList<LedgerEntry> Items, Guid? NextCursor);

public enum AddOutcome
{
    Added,
    KeyAlreadyClaimed,
    AlreadyReversed,
}

public interface IEntryStore
{
    /// <summary>Persists the entry, its event and the idempotency claim atomically.</summary>
    Task<AddOutcome> AddAsync(LedgerEntry entry, EntryRecorded recorded, IdempotencyClaim claim, CancellationToken cancellationToken);

    Task<LedgerEntry?> FindAsync(Guid merchantId, Guid id, CancellationToken cancellationToken);

    Task<ClaimedEntry?> FindClaimAsync(Guid merchantId, string key, CancellationToken cancellationToken);

    Task<EntryPage> ListAsync(Guid merchantId, DateOnly date, Guid? cursor, int limit, CancellationToken cancellationToken);

    /// <summary>Reads every entry in identity order, starting after the given entry.</summary>
    Task<IReadOnlyList<LedgerEntry>> ReadBatchAsync(Guid? after, int batchSize, CancellationToken cancellationToken);

    /// <summary>Marks the event as pending, creating it when retention already removed it.</summary>
    Task RequeueAsync(EntryRecorded recorded, CancellationToken cancellationToken);
}
