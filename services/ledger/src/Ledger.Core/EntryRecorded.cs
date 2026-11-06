// @anchors
//   code: ERECD
//   ref: RCENT
//   updated_at: 2026-11-05
namespace Ledger.Core;

/// <summary>Integration event defined by docs/contratos/entry-recorded.v1.schema.json.</summary>
public sealed record EntryRecorded(
    Guid EventId,
    string Type,
    int Version,
    DateTimeOffset OccurredAt,
    Guid MerchantId,
    Guid EntryId,
    EntryType EntryType,
    long AmountCents,
    DateOnly AccountingDate,
    Guid? ReversalOf)
{
    public const string EventType = "entry.recorded";

    public static EntryRecorded From(LedgerEntry entry) => new(
        Guid.CreateVersion7(entry.CreatedAt),
        EventType,
        1,
        entry.CreatedAt,
        entry.MerchantId,
        entry.Id,
        entry.EntryType,
        entry.AmountCents,
        entry.AccountingDate,
        entry.ReversalOf);
}
