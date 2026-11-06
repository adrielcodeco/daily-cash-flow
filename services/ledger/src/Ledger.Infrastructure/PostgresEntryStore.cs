// @anchors
//   code: PGEST
//   ref: RCENT
//   updated_at: 2026-11-06
using System.Text.Json;
using Ledger.Core;
using Npgsql;
using NpgsqlTypes;

namespace Ledger.Infrastructure;

public sealed class PostgresEntryStore(NpgsqlDataSource dataSource) : IEntryStore
{
    private const string UniqueViolation = "23505";
    private const string ReversalIndex = "ux_ledger_entry_reversal_of";
    private const string EntryColumns = "id, merchant_id, entry_type, amount_cents, accounting_date, description, reversal_of, created_at";

    private static readonly JsonSerializerOptions EventJson = new(JsonSerializerDefaults.Web);

    public async Task<AddOutcome> AddAsync(LedgerEntry entry, EntryRecorded recorded, IdempotencyClaim claim, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using (var insertEntry = new NpgsqlCommand(
                $"INSERT INTO ledger_entry ({EntryColumns}) VALUES ($1, $2, $3, $4, $5, $6, $7, $8)", connection, transaction))
            {
                insertEntry.Parameters.Add(new() { Value = entry.Id });
                insertEntry.Parameters.Add(new() { Value = entry.MerchantId });
                insertEntry.Parameters.Add(new() { Value = ToColumn(entry.EntryType) });
                insertEntry.Parameters.Add(new() { Value = entry.AmountCents });
                insertEntry.Parameters.Add(new() { Value = entry.AccountingDate });
                insertEntry.Parameters.Add(new() { Value = (object?)entry.Description ?? DBNull.Value, NpgsqlDbType = NpgsqlDbType.Text });
                insertEntry.Parameters.Add(new() { Value = (object?)entry.ReversalOf ?? DBNull.Value, NpgsqlDbType = NpgsqlDbType.Uuid });
                insertEntry.Parameters.Add(new() { Value = entry.CreatedAt.ToUniversalTime() });
                await insertEntry.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var insertClaim = new NpgsqlCommand(
                "INSERT INTO idempotency_key (merchant_id, key, request_hash, entry_id, created_at) VALUES ($1, $2, $3, $4, $5)",
                connection, transaction))
            {
                insertClaim.Parameters.Add(new() { Value = entry.MerchantId });
                insertClaim.Parameters.Add(new() { Value = claim.Key });
                insertClaim.Parameters.Add(new() { Value = claim.RequestHash });
                insertClaim.Parameters.Add(new() { Value = entry.Id });
                insertClaim.Parameters.Add(new() { Value = entry.CreatedAt.ToUniversalTime() });
                await insertClaim.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var insertEvent = new NpgsqlCommand(
                "INSERT INTO outbox (id, type, payload, created_at) VALUES ($1, $2, $3, $4)", connection, transaction))
            {
                AddEventParameters(insertEvent, recorded);
                await insertEvent.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return AddOutcome.Added;
        }
        catch (PostgresException exception) when (exception.SqlState == UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            return exception.ConstraintName == ReversalIndex ? AddOutcome.AlreadyReversed : AddOutcome.KeyAlreadyClaimed;
        }
    }

    public async Task<LedgerEntry?> FindAsync(Guid merchantId, Guid id, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand($"SELECT {EntryColumns} FROM ledger_entry WHERE id = $1 AND merchant_id = $2");
        command.Parameters.Add(new() { Value = id });
        command.Parameters.Add(new() { Value = merchantId });

        var entries = await ReadEntriesAsync(command, cancellationToken);
        return entries.Count == 0 ? null : entries[0];
    }

    public async Task<ClaimedEntry?> FindClaimAsync(Guid merchantId, string key, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("SELECT request_hash, entry_id FROM idempotency_key WHERE merchant_id = $1 AND key = $2");
        command.Parameters.Add(new() { Value = merchantId });
        command.Parameters.Add(new() { Value = key });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? new ClaimedEntry(reader.GetString(0), reader.GetGuid(1)) : null;
    }

    public async Task<EntryPage> ListAsync(Guid merchantId, DateOnly date, Guid? cursor, int limit, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            $"""
            SELECT {EntryColumns}
            FROM ledger_entry
            WHERE merchant_id = $1 AND accounting_date = $2 AND ($3::uuid IS NULL OR id > $3)
            ORDER BY id
            LIMIT $4
            """);
        command.Parameters.Add(new() { Value = merchantId });
        command.Parameters.Add(new() { Value = date });
        command.Parameters.Add(new() { Value = (object?)cursor ?? DBNull.Value, NpgsqlDbType = NpgsqlDbType.Uuid });
        command.Parameters.Add(new() { Value = limit + 1 });

        var entries = await ReadEntriesAsync(command, cancellationToken);
        var hasMore = entries.Count > limit;
        var items = hasMore ? entries.GetRange(0, limit) : entries;
        return new EntryPage(items, hasMore ? items[^1].Id : null);
    }

    public async Task<IReadOnlyList<LedgerEntry>> ReadBatchAsync(Guid? after, int batchSize, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            $"SELECT {EntryColumns} FROM ledger_entry WHERE ($1::uuid IS NULL OR id > $1) ORDER BY id LIMIT $2");
        command.Parameters.Add(new() { Value = (object?)after ?? DBNull.Value, NpgsqlDbType = NpgsqlDbType.Uuid });
        command.Parameters.Add(new() { Value = batchSize });

        return await ReadEntriesAsync(command, cancellationToken);
    }

    public async Task RequeueAsync(EntryRecorded recorded, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO outbox (id, type, payload, created_at) VALUES ($1, $2, $3, $4)
            ON CONFLICT (id) DO UPDATE SET published_at = NULL
            """);
        AddEventParameters(command, recorded);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddEventParameters(NpgsqlCommand command, EntryRecorded recorded)
    {
        command.Parameters.Add(new() { Value = recorded.EventId });
        command.Parameters.Add(new() { Value = recorded.Type });
        command.Parameters.Add(new() { Value = JsonSerializer.Serialize(recorded, EventJson), NpgsqlDbType = NpgsqlDbType.Jsonb });
        command.Parameters.Add(new() { Value = recorded.OccurredAt.ToUniversalTime() });
    }

    private static async Task<List<LedgerEntry>> ReadEntriesAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        var entries = new List<LedgerEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new LedgerEntry(
                reader.GetGuid(0),
                reader.GetGuid(1),
                FromColumn(reader.GetString(2)),
                reader.GetInt64(3),
                reader.GetFieldValue<DateOnly>(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetGuid(6),
                reader.GetFieldValue<DateTimeOffset>(7)));
        }

        return entries;
    }

    private static string ToColumn(EntryType type) => type switch
    {
        EntryType.Credit => "CREDIT",
        EntryType.Debit => "DEBIT",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown entry type."),
    };

    private static EntryType FromColumn(string value) => value switch
    {
        "CREDIT" => EntryType.Credit,
        "DEBIT" => EntryType.Debit,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown entry type."),
    };
}
