// @anchors
//   code: CNFKS
//   layer: test
//   updated_at: 2026-11-05
using Consolidation.Core;

namespace Consolidation.Tests;

internal sealed class InMemoryBalanceStore : IBalanceStore
{
    private readonly HashSet<Guid> applied = [];
    private readonly Dictionary<(Guid, DateOnly), (long Credits, long Debits)> totals = [];

    public int DailyQueries { get; private set; }

    public Guid? PeriodMerchant { get; private set; }

    public (long Credits, long Debits) Totals(Guid merchantId, DateOnly date) =>
        totals.GetValueOrDefault((merchantId, date));

    public Task<bool> ApplyOnceAsync(BalanceChange change, CancellationToken cancellationToken)
    {
        if (!applied.Add(change.EventId))
        {
            return Task.FromResult(false);
        }

        var current = Totals(change.MerchantId, change.Date);
        totals[(change.MerchantId, change.Date)] = (current.Credits + change.CreditsCents, current.Debits + change.DebitsCents);
        return Task.FromResult(true);
    }

    public Task<DailyBalance> GetDailyAsync(Guid merchantId, DateOnly date, CancellationToken cancellationToken)
    {
        DailyQueries++;
        var (credits, debits) = Totals(merchantId, date);
        return Task.FromResult(new DailyBalance(date, 0, credits, debits, null));
    }

    public Task<PeriodTotals> GetPeriodAsync(Guid merchantId, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        PeriodMerchant = merchantId;
        var days = totals
            .Where(t => t.Key.Item1 == merchantId && t.Key.Item2 >= start && t.Key.Item2 <= end)
            .Select(t => new DayTotals(t.Key.Item2, t.Value.Credits, t.Value.Debits))
            .ToList();
        return Task.FromResult(new PeriodTotals(0, days, null));
    }

    public Task ResetAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
}

internal sealed class InMemoryBalanceCache : IBalanceCache
{
    private readonly Dictionary<(Guid, DateOnly), DailyBalance> entries = [];

    public List<Guid> Invalidated { get; } = [];

    public Task<DailyBalance?> GetAsync(Guid merchantId, DateOnly date, CancellationToken cancellationToken) =>
        Task.FromResult(entries.GetValueOrDefault((merchantId, date)));

    public Task SetAsync(Guid merchantId, DailyBalance balance, CancellationToken cancellationToken)
    {
        entries[(merchantId, balance.Date)] = balance;
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(Guid merchantId, CancellationToken cancellationToken)
    {
        Invalidated.Add(merchantId);
        return Task.CompletedTask;
    }
}

internal sealed class BrokenBalanceCache : IBalanceCache
{
    public Task<DailyBalance?> GetAsync(Guid merchantId, DateOnly date, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Cache unavailable");

    public Task SetAsync(Guid merchantId, DailyBalance balance, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Cache unavailable");

    public Task InvalidateAsync(Guid merchantId, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Cache unavailable");
}
