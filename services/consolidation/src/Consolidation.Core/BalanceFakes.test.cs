// @anchors
//   code: CNFKS
//   layer: test
//   updated_at: 2026-11-06
using Consolidation.Core;

namespace Consolidation.Tests;

internal sealed class InMemoryBalanceStore : IBalanceStore
{
    private readonly HashSet<Guid> applied = [];
    private readonly Dictionary<(Guid, DateOnly), (long Credits, long Debits)> totals = [];

    public int AppliedEvents => applied.Count;

    public int DailyQueries { get; private set; }

    public Guid? PeriodMerchant { get; private set; }

    public int PeriodQueries { get; private set; }

    /// <summary>Simulates an invalidation that happens while the query runs.</summary>
    public Action? DuringQuery { get; set; }

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
        DuringQuery?.Invoke();
        var (credits, debits) = Totals(merchantId, date);
        return Task.FromResult(new DailyBalance(date, 0, credits, debits, null));
    }

    public Task<PeriodTotals> GetPeriodAsync(Guid merchantId, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        PeriodMerchant = merchantId;
        PeriodQueries++;
        var days = totals
            .Where(t => t.Key.Item1 == merchantId && t.Key.Item2 >= start && t.Key.Item2 <= end)
            .Select(t => new DayTotals(t.Key.Item2, t.Value.Credits, t.Value.Debits))
            .ToList();
        return Task.FromResult(new PeriodTotals(0, days, null));
    }

    public Task ResetAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
}

internal sealed class InMemoryQueryCache : IQueryCache
{
    private readonly Dictionary<(Guid, string, string), object> entries = [];
    private readonly Dictionary<Guid, int> versions = [];

    public List<Guid> Invalidated { get; } = [];

    public int Reads { get; private set; }

    public Task<CacheLookup<T>> GetAsync<T>(Guid merchantId, string key, CancellationToken cancellationToken)
        where T : class
    {
        Reads++;
        var version = versions.GetValueOrDefault(merchantId).ToString();
        return Task.FromResult(new CacheLookup<T>(entries.GetValueOrDefault((merchantId, version, key)) as T, version));
    }

    public Task SetAsync<T>(Guid merchantId, string key, string version, T value, CancellationToken cancellationToken)
        where T : class
    {
        entries[(merchantId, version, key)] = value;
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(Guid merchantId, CancellationToken cancellationToken)
    {
        Invalidated.Add(merchantId);
        versions[merchantId] = versions.GetValueOrDefault(merchantId) + 1;
        return Task.CompletedTask;
    }
}

internal sealed class BrokenQueryCache : IQueryCache
{
    public Task<CacheLookup<T>> GetAsync<T>(Guid merchantId, string key, CancellationToken cancellationToken)
        where T : class => throw new InvalidOperationException("Cache unavailable");

    public Task SetAsync<T>(Guid merchantId, string key, string version, T value, CancellationToken cancellationToken)
        where T : class => throw new InvalidOperationException("Cache unavailable");

    public Task InvalidateAsync(Guid merchantId, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Cache unavailable");
}

internal sealed class WriteFailingQueryCache : IQueryCache
{
    public Task<CacheLookup<T>> GetAsync<T>(Guid merchantId, string key, CancellationToken cancellationToken)
        where T : class => Task.FromResult(new CacheLookup<T>(null, "0"));

    public Task SetAsync<T>(Guid merchantId, string key, string version, T value, CancellationToken cancellationToken)
        where T : class => throw new InvalidOperationException("Cache unavailable");

    public Task InvalidateAsync(Guid merchantId, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class CancelledQueryCache : IQueryCache
{
    public Task<CacheLookup<T>> GetAsync<T>(Guid merchantId, string key, CancellationToken cancellationToken)
        where T : class => throw new OperationCanceledException();

    public Task SetAsync<T>(Guid merchantId, string key, string version, T value, CancellationToken cancellationToken)
        where T : class => throw new OperationCanceledException();

    public Task InvalidateAsync(Guid merchantId, CancellationToken cancellationToken) =>
        throw new OperationCanceledException();
}

internal sealed class RecordingQueryCache : IQueryCache
{
    public List<string> Keys { get; } = [];

    public Task<CacheLookup<T>> GetAsync<T>(Guid merchantId, string key, CancellationToken cancellationToken)
        where T : class
    {
        Keys.Add(key);
        return Task.FromResult(new CacheLookup<T>(null, "0"));
    }

    public Task SetAsync<T>(Guid merchantId, string key, string version, T value, CancellationToken cancellationToken)
        where T : class => Task.CompletedTask;

    public Task InvalidateAsync(Guid merchantId, CancellationToken cancellationToken) => Task.CompletedTask;
}
