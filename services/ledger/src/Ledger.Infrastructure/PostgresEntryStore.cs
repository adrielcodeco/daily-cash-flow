// @anchors
//   code: PGEST
//   ref: RCENT
//   updated_at: 2026-11-05
using System.Text.Json;
using Ledger.Core;
using Npgsql;
using NpgsqlTypes;

namespace Ledger.Infrastructure;

public sealed class PostgresEntryStore(NpgsqlDataSource dataSource) : IEntryStore
{
    private static readonly JsonSerializerOptions EventJson = new(JsonSerializerDefaults.Web);

    public async Task AddAsync(LedgerEntry entry, EntryRecorded recorded, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var insertEntry = new NpgsqlCommand(
            """
            INSERT INTO ledger_entry (id, merchant_id, entry_type, amount_cents, accounting_date, description, reversal_of, created_at)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
            """, connection, transaction))
        {
            insertEntry.Parameters.Add(new() { Value = entry.Id });
            insertEntry.Parameters.Add(new() { Value = entry.MerchantId });
            insertEntry.Parameters.Add(new() { Value = ToColumn(entry.EntryType) });
            insertEntry.Parameters.Add(new() { Value = entry.AmountCents });
            insertEntry.Parameters.Add(new() { Value = entry.AccountingDate });
            insertEntry.Parameters.Add(new() { Value = (object?)entry.Description ?? DBNull.Value, NpgsqlDbType = NpgsqlDbType.Text });
            insertEntry.Parameters.Add(new() { Value = (object?)entry.ReversalOf ?? DBNull.Value, NpgsqlDbType = NpgsqlDbType.Uuid });
            insertEntry.Parameters.Add(new() { Value = entry.CreatedAt });
            await insertEntry.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var insertEvent = new NpgsqlCommand(
            "INSERT INTO outbox (id, type, payload, created_at) VALUES ($1, $2, $3, $4)", connection, transaction))
        {
            insertEvent.Parameters.Add(new() { Value = recorded.EventId });
            insertEvent.Parameters.Add(new() { Value = recorded.Type });
            insertEvent.Parameters.Add(new() { Value = JsonSerializer.Serialize(recorded, EventJson), NpgsqlDbType = NpgsqlDbType.Jsonb });
            insertEvent.Parameters.Add(new() { Value = recorded.OccurredAt });
            await insertEvent.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<LedgerEntry?> FindAsync(Guid merchantId, Guid id, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT id, merchant_id, entry_type, amount_cents, accounting_date, description, reversal_of, created_at
            FROM ledger_entry
            WHERE id = $1 AND merchant_id = $2
            """);
        command.Parameters.Add(new() { Value = id });
        command.Parameters.Add(new() { Value = merchantId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new LedgerEntry(
            reader.GetGuid(0),
            reader.GetGuid(1),
            FromColumn(reader.GetString(2)),
            reader.GetInt64(3),
            reader.GetFieldValue<DateOnly>(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetGuid(6),
            reader.GetFieldValue<DateTimeOffset>(7));
    }

    private static string ToColumn(EntryType type) => type == EntryType.Credit ? "CREDIT" : "DEBIT";

    private static EntryType FromColumn(string value) => value == "CREDIT" ? EntryType.Credit : EntryType.Debit;
}
