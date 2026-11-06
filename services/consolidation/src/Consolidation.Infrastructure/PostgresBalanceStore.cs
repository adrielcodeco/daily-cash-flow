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

    public async Task<PeriodTotals> GetPeriodAsync(Guid merchantId, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        await using var summary = dataSource.CreateCommand(
            """
            SELECT COALESCE(SUM(credits_cents - debits_cents) FILTER (WHERE date < $2), 0)::bigint, MAX(updated_at)
            FROM daily_balance
            WHERE merchant_id = $1
            """);
        summary.Parameters.Add(new() { Value = merchantId });
        summary.Parameters.Add(new() { Value = start });

        long opening;
        DateTimeOffset? updatedThrough;
        await using (var reader = await summary.ExecuteReaderAsync(cancellationToken))
        {
            await reader.ReadAsync(cancellationToken);
            opening = reader.GetInt64(0);
            updatedThrough = reader.IsDBNull(1) ? null : reader.GetFieldValue<DateTimeOffset>(1);
        }

        await using var daily = dataSource.CreateCommand(
            """
            SELECT date, credits_cents, debits_cents
            FROM daily_balance
            WHERE merchant_id = $1 AND date BETWEEN $2 AND $3
            ORDER BY date
            """);
        daily.Parameters.Add(new() { Value = merchantId });
        daily.Parameters.Add(new() { Value = start });
        daily.Parameters.Add(new() { Value = end });

        var days = new List<DayTotals>();
        await using (var reader = await daily.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                days.Add(new DayTotals(reader.GetFieldValue<DateOnly>(0), reader.GetInt64(1), reader.GetInt64(2)));
            }
        }

        return new PeriodTotals(opening, days, updatedThrough);
    }

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand("TRUNCATE daily_balance, processed_event");
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
