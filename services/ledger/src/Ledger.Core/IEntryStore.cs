// @anchors
//   code: IESTR
//   ref: RCENT
//   updated_at: 2026-11-06
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
