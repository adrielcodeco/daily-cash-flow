// @anchors
//   code: LDFKS
//   layer: test
//   updated_at: 2026-11-05
using Ledger.Core;

namespace Ledger.Tests;

internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class InMemoryEntryStore : IEntryStore
{
    private readonly Dictionary<(Guid, string), ClaimedEntry> claims = [];

    public List<LedgerEntry> Entries { get; } = [];

    public List<EntryRecorded> Events { get; } = [];

    public List<EntryRecorded> Requeued { get; } = [];

    /// <summary>Simulates a concurrent call that claims the key after the first lookup.</summary>
    public (string Key, ClaimedEntry Claim, LedgerEntry Entry)? RacingWinner { get; set; }

    public Task<AddOutcome> AddAsync(LedgerEntry entry, EntryRecorded recorded, IdempotencyClaim claim, CancellationToken cancellationToken)
    {
        if (RacingWinner is { } winner && winner.Key == claim.Key)
        {
            Entries.Add(winner.Entry);
            claims[(entry.MerchantId, claim.Key)] = winner.Claim;
            return Task.FromResult(AddOutcome.KeyAlreadyClaimed);
        }

        if (entry.ReversalOf is not null && Entries.Any(e => e.ReversalOf == entry.ReversalOf))
        {
            return Task.FromResult(AddOutcome.AlreadyReversed);
        }

        Entries.Add(entry);
        Events.Add(recorded);
        claims[(entry.MerchantId, claim.Key)] = new ClaimedEntry(claim.RequestHash, entry.Id);
        return Task.FromResult(AddOutcome.Added);
    }

    public Task<LedgerEntry?> FindAsync(Guid merchantId, Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Entries.FirstOrDefault(e => e.Id == id && e.MerchantId == merchantId));

    public Task<ClaimedEntry?> FindClaimAsync(Guid merchantId, string key, CancellationToken cancellationToken) =>
        Task.FromResult(claims.GetValueOrDefault((merchantId, key)));

    public Task<EntryPage> ListAsync(Guid merchantId, DateOnly date, Guid? cursor, int limit, CancellationToken cancellationToken)
    {
        var items = Entries
            .Where(e => e.MerchantId == merchantId && e.AccountingDate == date)
            .OrderBy(e => e.Id)
            .SkipWhile(e => cursor is not null && e.Id.CompareTo(cursor.Value) <= 0)
            .ToList();
        var page = items.Take(limit).ToList();
        return Task.FromResult(new EntryPage(page, items.Count > limit ? page[^1].Id : null));
    }

    public Task<IReadOnlyList<LedgerEntry>> ReadBatchAsync(Guid? after, int batchSize, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LedgerEntry>>(Entries
            .OrderBy(e => e.Id)
            .SkipWhile(e => after is not null && e.Id.CompareTo(after.Value) <= 0)
            .Take(batchSize)
            .ToList());

    public Task RequeueAsync(EntryRecorded recorded, CancellationToken cancellationToken)
    {
        Requeued.Add(recorded);
        return Task.CompletedTask;
    }
}
