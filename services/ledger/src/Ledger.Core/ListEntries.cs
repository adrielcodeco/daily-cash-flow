// @anchors
//   code: LSTCD
//   ref: LSTEN
//   updated_at: 2026-11-05
namespace Ledger.Core;

public sealed class ListEntries(IEntryStore store)
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    public Task<EntryPage> ExecuteAsync(Guid merchantId, DateOnly date, Guid? cursor, int limit, CancellationToken cancellationToken)
    {
        if (limit is < 1 or > MaxLimit)
        {
            throw new DomainRuleException("invalid_limit", "The limit must be between 1 and 200.", RuleKind.BadRequest);
        }

        return store.ListAsync(merchantId, date, cursor, limit, cancellationToken);
    }
}
