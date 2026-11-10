// @anchors
//   code: PGEST
//   ref: RCENT
//   updated_at: 2026-11-09
using System.Diagnostics;
using System.Text.Json;
using Dapper;
using Ledger.Core;
using Npgsql;

namespace Ledger.Infrastructure;

public sealed class PostgresEntryStore : IEntryStore
{
    private const string UniqueViolation = "23505";
    private const string ReversalIndex = "ux_ledger_entry_reversal_of";
    private const string EntryColumns = "id, merchant_id, entry_type, amount_cents, accounting_date, description, reversal_of, created_at";

    private static readonly JsonSerializerOptions EventJson = new(JsonSerializerDefaults.Web);

    private readonly NpgsqlDataSource dataSource;

    public PostgresEntryStore(NpgsqlDataSource dataSource)
    {
        DapperSetup.Configure();
        this.dataSource = dataSource;
    }

    public async Task<AddOutcome> AddAsync(LedgerEntry entry, EntryRecorded recorded, IdempotencyClaim claim, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await SetMerchantAsync(connection, transaction, entry.MerchantId, cancellationToken);

        try
        {
            await connection.ExecuteAsync(Command(
                $"""
                INSERT INTO ledger_entry ({EntryColumns})
                VALUES (@Id, @MerchantId, @EntryType, @AmountCents, @AccountingDate, @Description, @ReversalOf, @CreatedAt)
                """,
                new
                {
                    entry.Id,
                    entry.MerchantId,
                    EntryType = ToColumn(entry.EntryType),
                    entry.AmountCents,
                    entry.AccountingDate,
                    entry.Description,
                    entry.ReversalOf,
                    CreatedAt = entry.CreatedAt.ToUniversalTime(),
                },
                transaction,
                cancellationToken));

            await connection.ExecuteAsync(Command(
                """
                INSERT INTO idempotency_key (merchant_id, key, request_hash, entry_id, created_at)
                VALUES (@MerchantId, @Key, @RequestHash, @EntryId, @CreatedAt)
                """,
                new { entry.MerchantId, claim.Key, claim.RequestHash, EntryId = entry.Id, CreatedAt = entry.CreatedAt.ToUniversalTime() },
                transaction,
                cancellationToken));

            await connection.ExecuteAsync(Command(
                """
                INSERT INTO outbox (id, type, payload, created_at, trace_context)
                VALUES (@EventId, @Type, @Payload::jsonb, @OccurredAt, @TraceContext)
                """,
                new
                {
                    recorded.EventId,
                    recorded.Type,
                    Payload = JsonSerializer.Serialize(recorded, EventJson),
                    OccurredAt = recorded.OccurredAt.ToUniversalTime(),
                    TraceContext = Activity.Current?.Id,
                },
                transaction,
                cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return AddOutcome.Added;
        }
        catch (PostgresException exception) when (exception.SqlState == UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            return exception.ConstraintName == ReversalIndex ? AddOutcome.AlreadyReversed : AddOutcome.KeyAlreadyClaimed;
        }
    }

    public Task<LedgerEntry?> FindAsync(Guid merchantId, Guid id, CancellationToken cancellationToken) =>
        AsMerchantAsync(merchantId, async (connection, transaction) =>
        {
            var row = await connection.QuerySingleOrDefaultAsync<EntryRow>(Command(
                $"SELECT {EntryColumns} FROM ledger_entry WHERE id = @Id AND merchant_id = @MerchantId",
                new { Id = id, MerchantId = merchantId },
                transaction,
                cancellationToken));
            return row?.ToEntry();
        }, cancellationToken);

    public Task<ClaimedEntry?> FindClaimAsync(Guid merchantId, string key, CancellationToken cancellationToken) =>
        AsMerchantAsync(merchantId, async (connection, transaction) =>
        {
            var row = await connection.QuerySingleOrDefaultAsync<ClaimRow>(Command(
                "SELECT request_hash, entry_id FROM idempotency_key WHERE merchant_id = @MerchantId AND key = @Key",
                new { MerchantId = merchantId, Key = key },
                transaction,
                cancellationToken));
            return row is null ? null : new ClaimedEntry(row.RequestHash, row.EntryId);
        }, cancellationToken);

    public Task<EntryPage> ListAsync(Guid merchantId, DateOnly date, Guid? cursor, int limit, CancellationToken cancellationToken) =>
        AsMerchantAsync(merchantId, async (connection, transaction) =>
        {
            var rows = await connection.QueryAsync<EntryRow>(Command(
                $"""
                SELECT {EntryColumns}
                FROM ledger_entry
                WHERE merchant_id = @MerchantId AND accounting_date = @Date AND (@Cursor::uuid IS NULL OR id > @Cursor)
                ORDER BY id
                LIMIT @Limit
                """,
                new { MerchantId = merchantId, Date = date, Cursor = cursor, Limit = limit + 1 },
                transaction,
                cancellationToken));

            var entries = rows.Select(row => row.ToEntry()).ToList();
            var hasMore = entries.Count > limit;
            var items = hasMore ? entries.GetRange(0, limit) : entries;
            return new EntryPage(items, hasMore ? items[^1].Id : null);
        }, cancellationToken);

    public async Task<IReadOnlyList<LedgerEntry>> ReadBatchAsync(Guid? after, int batchSize, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<EntryRow>(Command(
            $"SELECT {EntryColumns} FROM ledger_entry WHERE (@After::uuid IS NULL OR id > @After) ORDER BY id LIMIT @BatchSize",
            new { After = after, BatchSize = batchSize },
            null,
            cancellationToken));

        return rows.Select(row => row.ToEntry()).ToList();
    }

    public async Task RequeueAsync(IReadOnlyList<EntryRecorded> recorded, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(Command(
            """
            INSERT INTO outbox (id, type, payload, created_at)
            SELECT id, type, payload::jsonb, created_at
            FROM unnest(@Ids, @Types, @Payloads, @OccurredAts) AS pending(id, type, payload, created_at)
            ON CONFLICT (id) DO UPDATE SET published_at = NULL, attempts = 0, next_attempt_at = '-infinity'
            """,
            new
            {
                Ids = recorded.Select(r => r.EventId).ToArray(),
                Types = recorded.Select(r => r.Type).ToArray(),
                Payloads = recorded.Select(r => JsonSerializer.Serialize(r, EventJson)).ToArray(),
                OccurredAts = recorded.Select(r => r.OccurredAt.ToUniversalTime()).ToArray(),
            },
            null,
            cancellationToken));
    }

    private static CommandDefinition Command(string sql, object parameters, NpgsqlTransaction? transaction, CancellationToken cancellationToken) =>
        new(sql, parameters, transaction, cancellationToken: cancellationToken);

    /// <summary>Row-level security reads the merchant from the transaction, so reads also need one.</summary>
    private async Task<T> AsMerchantAsync<T>(
        Guid merchantId,
        Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> query,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await SetMerchantAsync(connection, transaction, merchantId, cancellationToken);

        var result = await query(connection, transaction);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static Task SetMerchantAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid merchantId, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(Command(
            "SELECT set_config('app.merchant_id', @MerchantId, true)",
            new { MerchantId = merchantId.ToString() },
            transaction,
            cancellationToken));

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

    private sealed class EntryRow
    {
        public Guid Id { get; init; }

        public Guid MerchantId { get; init; }

        public string EntryType { get; init; } = string.Empty;

        public long AmountCents { get; init; }

        public DateOnly AccountingDate { get; init; }

        public string? Description { get; init; }

        public Guid? ReversalOf { get; init; }

        public DateTime CreatedAt { get; init; }

        public LedgerEntry ToEntry() => new(
            Id, MerchantId, FromColumn(EntryType), AmountCents, AccountingDate, Description, ReversalOf, new DateTimeOffset(CreatedAt));
    }

    private sealed class ClaimRow
    {
        public string RequestHash { get; init; } = string.Empty;

        public Guid EntryId { get; init; }
    }
}
