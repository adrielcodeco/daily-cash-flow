// @anchors
//   code: RCECD
//   ref: RCENT
//   updated_at: 2026-11-09
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
        return writer.WriteAsync(
            command.MerchantId,
            new IdempotencyClaim(key, HashOf(command)),
            () => Task.FromResult(LedgerEntry.Record(
                command.MerchantId,
                command.EntryType,
                command.AmountCents,
                command.AccountingDate,
                command.Description,
                clock)),
            cancellationToken);
    }

    /// <summary>The length tells an empty description apart from none, which the stored entry also keeps apart.</summary>
    public static string HashOf(RecordEntryCommand command) =>
        IdempotentWriter.HashOf(
            $"record|{command.EntryType}|{command.AmountCents}|{command.AccountingDate:O}|{command.Description?.Length ?? -1}|{command.Description}");
}
