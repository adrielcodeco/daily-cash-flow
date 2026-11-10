// @anchors
//   code: PGBST
//   ref: APENT
//   updated_at: 2026-11-09
using Consolidation.Core;
using Dapper;
using Npgsql;

namespace Consolidation.Infrastructure;

public sealed class PostgresBalanceStore : IBalanceStore
{
    private readonly NpgsqlDataSource dataSource;
    private readonly TimeProvider clock;

    public PostgresBalanceStore(NpgsqlDataSource dataSource, TimeProvider clock)
    {
        DapperSetup.Configure();
        this.dataSource = dataSource;
        this.clock = clock;
    }

    public async Task<bool> ApplyOnceAsync(BalanceChange change, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await SetMerchantAsync(connection, transaction, change.MerchantId, cancellationToken);

        var registered = await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO processed_event (event_id, processed_at) VALUES (@EventId, @ProcessedAt) ON CONFLICT DO NOTHING",
            new { change.EventId, ProcessedAt = clock.GetUtcNow() },
            transaction,
            cancellationToken: cancellationToken));
        if (registered == 0)
        {
            return false;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO daily_balance (merchant_id, date, credits_cents, debits_cents, updated_at)
            VALUES (@MerchantId, @Date, @CreditsCents, @DebitsCents, @UpdatedAt)
            ON CONFLICT (merchant_id, date) DO UPDATE SET
                credits_cents = daily_balance.credits_cents + EXCLUDED.credits_cents,
                debits_cents = daily_balance.debits_cents + EXCLUDED.debits_cents,
                updated_at = GREATEST(daily_balance.updated_at, EXCLUDED.updated_at)
            """,
            new { change.MerchantId, change.Date, change.CreditsCents, change.DebitsCents, UpdatedAt = change.OccurredAt.ToUniversalTime() },
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task<DailyBalance> GetDailyAsync(Guid merchantId, DateOnly date, CancellationToken cancellationToken) =>
        AsMerchantAsync(merchantId, async (connection, transaction) =>
        {
            var row = await connection.QuerySingleAsync<DailyRow>(new CommandDefinition(
                """
                SELECT
                    COALESCE(SUM(credits_cents - debits_cents) FILTER (WHERE date < @Date), 0)::bigint AS opening_cents,
                    COALESCE(SUM(credits_cents) FILTER (WHERE date = @Date), 0)::bigint AS credits_cents,
                    COALESCE(SUM(debits_cents) FILTER (WHERE date = @Date), 0)::bigint AS debits_cents,
                    MAX(updated_at) AS updated_at
                FROM daily_balance
                WHERE merchant_id = @MerchantId
                """,
                new { MerchantId = merchantId, Date = date },
                transaction,
                cancellationToken: cancellationToken));

            return new DailyBalance(date, row.OpeningCents, row.CreditsCents, row.DebitsCents, ToOffset(row.UpdatedAt));
        }, cancellationToken);

    public Task<PeriodTotals> GetPeriodAsync(Guid merchantId, DateOnly start, DateOnly end, CancellationToken cancellationToken) =>
        AsMerchantAsync(merchantId, async (connection, transaction) =>
        {
            var summary = await connection.QuerySingleAsync<DailyRow>(new CommandDefinition(
                """
                SELECT COALESCE(SUM(credits_cents - debits_cents) FILTER (WHERE date < @Start), 0)::bigint AS opening_cents,
                       MAX(updated_at) AS updated_at
                FROM daily_balance
                WHERE merchant_id = @MerchantId
                """,
                new { MerchantId = merchantId, Start = start },
                transaction,
                cancellationToken: cancellationToken));

            var days = await connection.QueryAsync<DayRow>(new CommandDefinition(
                """
                SELECT date, credits_cents, debits_cents
                FROM daily_balance
                WHERE merchant_id = @MerchantId AND date BETWEEN @Start AND @End
                ORDER BY date
                """,
                new { MerchantId = merchantId, Start = start, End = end },
                transaction,
                cancellationToken: cancellationToken));

            var totals = days.Select(day => new DayTotals(day.Date, day.CreditsCents, day.DebitsCents)).ToList();
            return new PeriodTotals(summary.OpeningCents, totals, ToOffset(summary.UpdatedAt));
        }, cancellationToken);

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition("TRUNCATE daily_balance, processed_event", cancellationToken: cancellationToken));
    }

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

    /// <summary>The consumer also writes as the event's merchant, so row-level security holds for it too.</summary>
    private static Task SetMerchantAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid merchantId, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition(
            "SELECT set_config('app.merchant_id', @MerchantId, true)",
            new { MerchantId = merchantId.ToString() },
            transaction,
            cancellationToken: cancellationToken));

    private static DateTimeOffset? ToOffset(DateTime? value) => value is null ? null : new DateTimeOffset(value.Value);

    private sealed class DailyRow
    {
        public long OpeningCents { get; init; }

        public long CreditsCents { get; init; }

        public long DebitsCents { get; init; }

        public DateTime? UpdatedAt { get; init; }
    }

    private sealed class DayRow
    {
        public DateOnly Date { get; init; }

        public long CreditsCents { get; init; }

        public long DebitsCents { get; init; }
    }
}
