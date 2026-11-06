// @anchors
//   code: IDWRT
//   ref: RCENT
//   updated_at: 2026-11-05
using System.Security.Cryptography;
using System.Text;

namespace Ledger.Core;

public sealed class IdempotentWriter(IEntryStore store)
{
    public static string HashOf(string canonicalRequest) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));

    public async Task<LedgerEntry> WriteAsync(
        Guid merchantId,
        IdempotencyClaim claim,
        Func<Task<LedgerEntry>> createEntry,
        CancellationToken cancellationToken)
    {
        var replay = await ReplayAsync(merchantId, claim, cancellationToken);
        if (replay is not null)
        {
            return replay;
        }

        var entry = await createEntry();
        var outcome = await store.AddAsync(entry, EntryRecorded.From(entry), claim, cancellationToken);

        return outcome switch
        {
            AddOutcome.Added => entry,
            AddOutcome.AlreadyReversed => throw new DomainRuleException("already_reversed", "The entry was already reversed.", RuleKind.Conflict),
            _ => await ReplayAsync(merchantId, claim, cancellationToken)
                ?? throw new InvalidOperationException("The idempotency claim disappeared."),
        };
    }

    private async Task<LedgerEntry?> ReplayAsync(Guid merchantId, IdempotencyClaim claim, CancellationToken cancellationToken)
    {
        var claimed = await store.FindClaimAsync(merchantId, claim.Key, cancellationToken);
        if (claimed is null)
        {
            return null;
        }

        if (claimed.RequestHash != claim.RequestHash)
        {
            throw new DomainRuleException("idempotency_key_reused", "The idempotency key was used with different content.", RuleKind.Conflict);
        }

        return await store.FindAsync(merchantId, claimed.EntryId, cancellationToken)
            ?? throw new InvalidOperationException("The claimed entry disappeared.");
    }
}
