// @anchors
//   code: PGBST
//   ref: APENT
//   updated_at: 2026-11-05
using Consolidation.Core;
using Npgsql;

namespace Consolidation.Infrastructure;

public sealed class PostgresBalanceStore(NpgsqlDataSource dataSource, TimeProvider clock) : IBalanceStore
{
    public async Task<bool> ApplyOnceAsync(BalanceChange change, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var registerEvent = new NpgsqlCommand(
            "INSERT INTO processed_event (event_id, processed_at) VALUES ($1, $2) ON CONFLICT DO NOTHING", connection, transaction))
        {
            registerEvent.Parameters.Add(new() { Value = change.EventId });
            registerEvent.Parameters.Add(new() { Value = clock.GetUtcNow() });
            if (await registerEvent.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                return false;
            }
        }

        await using (var upsert = new NpgsqlCommand(
            """
            INSERT INTO daily_balance (merchant_id, date, credits_cents, debits_cents, updated_at)
            VALUES ($1, $2, $3, $4, $5)
            ON CONFLICT (merchant_id, date) DO UPDATE SET
                credits_cents = daily_balance.credits_cents + EXCLUDED.credits_cents,
                debits_cents = daily_balance.debits_cents + EXCLUDED.debits_cents,
                updated_at = GREATEST(daily_balance.updated_at, EXCLUDED.updated_at)
            """, connection, transaction))
        {
            upsert.Parameters.Add(new() { Value = change.MerchantId });
            upsert.Parameters.Add(new() { Value = change.Date });
            upsert.Parameters.Add(new() { Value = change.CreditsCents });
            upsert.Parameters.Add(new() { Value = change.DebitsCents });
            upsert.Parameters.Add(new() { Value = change.OccurredAt.ToUniversalTime() });
            await upsert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<DailyBalance> GetDailyAsync(Guid merchantId, DateOnly date, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT
                COALESCE(SUM(credits_cents - debits_cents) FILTER (WHERE date < $2), 0)::bigint,
                COALESCE(SUM(credits_cents) FILTER (WHERE date = $2), 0)::bigint,
                COALESCE(SUM(debits_cents) FILTER (WHERE date = $2), 0)::bigint,
                MAX(updated_at)
            FROM daily_balance
            WHERE merchant_id = $1
            """);
        command.Parameters.Add(new() { Value = merchantId });
        command.Parameters.Add(new() { Value = date });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        return new DailyBalance(
            date,
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2),
            reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3));
    }
}
