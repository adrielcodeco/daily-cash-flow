// @anchors
//   code: GTBCD
//   ref: GTBAL
//   updated_at: 2026-11-06
namespace Consolidation.Core;

public sealed class GetDailyBalance(IBalanceStore store)
{
    public Task<DailyBalance> ExecuteAsync(Guid merchantId, DateOnly date, CancellationToken cancellationToken) =>
        store.GetDailyAsync(merchantId, date, cancellationToken);
}
