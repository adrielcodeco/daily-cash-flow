// @anchors
//   code: RVECD
//   ref: RVENT
//   updated_at: 2026-11-05
namespace Ledger.Core;

public sealed class ReverseEntry(IEntryStore store, TimeProvider clock)
{
    private readonly IdempotentWriter writer = new(store);

    public Task<LedgerEntry> ExecuteAsync(Guid merchantId, Guid entryId, string key, CancellationToken cancellationToken) =>
        writer.WriteAsync(
            merchantId,
            new IdempotencyClaim(key, IdempotentWriter.HashOf($"reverse|{entryId}")),
            async () =>
            {
                var original = await store.FindAsync(merchantId, entryId, cancellationToken)
                    ?? throw new DomainRuleException("entry_not_found", "The entry was not found.", RuleKind.NotFound);

                return LedgerEntry.Reverse(original, clock);
            },
            cancellationToken);
}
