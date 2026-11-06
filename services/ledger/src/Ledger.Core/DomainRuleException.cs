// @anchors
//   code: DREXC
//   ref: LEDEN
//   updated_at: 2026-11-05
namespace Ledger.Core;

public enum RuleKind
{
    Invalid,
    BadRequest,
    NotFound,
    Conflict,
}

public sealed class DomainRuleException(string code, string message, RuleKind kind = RuleKind.Invalid) : Exception(message)
{
    public string Code { get; } = code;

    public RuleKind Kind { get; } = kind;
}
