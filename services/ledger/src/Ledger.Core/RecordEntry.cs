// @anchors
//   code: RCECD
//   ref: RCENT
//   updated_at: 2026-11-05
namespace Ledger.Core;

public sealed record RecordEntryCommand(
    Guid MerchantId,
    EntryType EntryType,
    long AmountCents,
    DateOnly AccountingDate,
    string? Description);

public sealed class RecordEntry(IEntryStore store, TimeProvider clock)
{
    public async Task<LedgerEntry> ExecuteAsync(RecordEntryCommand command, CancellationToken cancellationToken)
    {
        var entry = LedgerEntry.Record(
            command.MerchantId,
            command.EntryType,
            command.AmountCents,
            command.AccountingDate,
            command.Description,
            clock);

        await store.AddAsync(entry, EntryRecorded.From(entry), cancellationToken);
        return entry;
    }
}
