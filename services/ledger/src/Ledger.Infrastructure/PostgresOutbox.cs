// @anchors
//   code: PGOBX
//   ref: OBRLY
//   updated_at: 2026-11-06
using Dapper;
using Ledger.Core;
using Npgsql;

namespace Ledger.Infrastructure;

public sealed class PostgresOutbox : IOutbox
{
    private readonly NpgsqlDataSource dataSource;
    private readonly TimeProvider clock;

    public PostgresOutbox(NpgsqlDataSource dataSource, TimeProvider clock)
    {
        DapperSetup.Configure();
        this.dataSource = dataSource;
        this.clock = clock;
    }

    public async Task<IOutboxBatch> ClaimPendingAsync(int limit, CancellationToken cancellationToken)
    {
        var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var rows = await connection.QueryAsync<OutboxRow>(new CommandDefinition(
            """
            SELECT id, type, payload::text AS payload, trace_context
            FROM outbox
            WHERE published_at IS NULL
            ORDER BY created_at
            LIMIT @Limit
            FOR UPDATE SKIP LOCKED
            """,
            new { Limit = limit },
            transaction,
            cancellationToken: cancellationToken));

        var messages = rows.Select(row => new OutboxMessage(row.Id, row.Type, row.Payload, row.TraceContext)).ToList();
        return new Batch(connection, transaction, messages, clock);
    }

    public async Task<(long Pending, double OldestAgeSeconds)> ReadBacklogAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<(long, double)>(new CommandDefinition(
            """
            SELECT count(*), coalesce(extract(epoch FROM now() - min(created_at)), 0)::double precision
            FROM outbox
            WHERE published_at IS NULL
            """,
            cancellationToken: cancellationToken));
    }

    private sealed class OutboxRow
    {
        public Guid Id { get; init; }

        public string Type { get; init; } = string.Empty;

        public string Payload { get; init; } = string.Empty;

        public string? TraceContext { get; init; }
    }

    private sealed class Batch(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyList<OutboxMessage> messages,
        TimeProvider clock) : IOutboxBatch
    {
        public IReadOnlyList<OutboxMessage> Messages => messages;

        public Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken) =>
            connection.ExecuteAsync(new CommandDefinition(
                "UPDATE outbox SET published_at = @PublishedAt WHERE id = @Id",
                new { PublishedAt = clock.GetUtcNow(), Id = id },
                transaction,
                cancellationToken: cancellationToken));

        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

        public async ValueTask DisposeAsync()
        {
            await transaction.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
