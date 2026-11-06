// @anchors
//   code: DREXC
//   ref: LEDEN
//   updated_at: 2026-11-05
namespace Ledger.Core;

public sealed class DomainRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
