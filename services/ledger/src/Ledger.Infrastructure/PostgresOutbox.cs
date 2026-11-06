// @anchors
//   code: PGOBX
//   ref: OBRLY
//   updated_at: 2026-11-05
using Ledger.Core;
using Npgsql;

namespace Ledger.Infrastructure;

public sealed class PostgresOutbox(NpgsqlDataSource dataSource, TimeProvider clock) : IOutbox
{
    public async Task<IOutboxBatch> ClaimPendingAsync(int limit, CancellationToken cancellationToken)
    {
        var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            SELECT id, type, payload::text
            FROM outbox
            WHERE published_at IS NULL
            ORDER BY created_at
            LIMIT $1
            FOR UPDATE SKIP LOCKED
            """, connection, transaction);
        command.Parameters.Add(new() { Value = limit });

        var messages = new List<OutboxMessage>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                messages.Add(new OutboxMessage(reader.GetGuid(0), reader.GetString(1), reader.GetString(2)));
            }
        }

        return new Batch(connection, transaction, messages, clock);
    }

    private sealed class Batch(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyList<OutboxMessage> messages,
        TimeProvider clock) : IOutboxBatch
    {
        public IReadOnlyList<OutboxMessage> Messages => messages;

        public async Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken)
        {
            await using var command = new NpgsqlCommand(
                "UPDATE outbox SET published_at = $1 WHERE id = $2", connection, transaction);
            command.Parameters.Add(new() { Value = clock.GetUtcNow() });
            command.Parameters.Add(new() { Value = id });
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

        public async ValueTask DisposeAsync()
        {
            await transaction.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
