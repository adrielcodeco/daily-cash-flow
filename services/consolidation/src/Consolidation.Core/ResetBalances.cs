// @anchors
//   code: RSBCD
//   ref: RSBAL
//   updated_at: 2026-11-05
namespace Consolidation.Core;

public sealed class ResetBalances(IBalanceStore store)
{
    public Task ExecuteAsync(CancellationToken cancellationToken) => store.ResetAsync(cancellationToken);
}
