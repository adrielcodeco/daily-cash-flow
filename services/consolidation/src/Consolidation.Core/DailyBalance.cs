// @anchors
//   code: DYBCD
//   ref: DYBAL
//   updated_at: 2026-11-06
namespace Consolidation.Core;

public sealed record DailyBalance(
    DateOnly Date,
    long OpeningCents,
    long CreditsCents,
    long DebitsCents,
    DateTimeOffset? UpdatedThrough)
{
    public long ClosingCents => checked(OpeningCents + CreditsCents - DebitsCents);
}
