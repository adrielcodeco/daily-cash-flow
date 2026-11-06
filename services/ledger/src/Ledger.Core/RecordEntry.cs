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
    private readonly IdempotentWriter writer = new(store);

    public Task<LedgerEntry> ExecuteAsync(RecordEntryCommand command, string key, CancellationToken cancellationToken)
    {
        var hash = IdempotentWriter.HashOf(
            $"record|{command.EntryType}|{command.AmountCents}|{command.AccountingDate:O}|{command.Description}");

        return writer.WriteAsync(
            command.MerchantId,
            new IdempotencyClaim(key, hash),
            () => Task.FromResult(LedgerEntry.Record(
                command.MerchantId,
                command.EntryType,
                command.AmountCents,
                command.AccountingDate,
                command.Description,
                clock)),
            cancellationToken);
    }
}
