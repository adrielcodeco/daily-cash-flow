// @anchors
//   code: LEDCD
//   ref: LEDEN
//   updated_at: 2026-11-05
namespace Ledger.Core;

public sealed record LedgerEntry(
    Guid Id,
    Guid MerchantId,
    EntryType EntryType,
    long AmountCents,
    DateOnly AccountingDate,
    string? Description,
    Guid? ReversalOf,
    DateTimeOffset CreatedAt)
{
    public const int MaxDescriptionLength = 200;

    private static readonly TimeZoneInfo BusinessTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static LedgerEntry Record(
        Guid merchantId,
        EntryType entryType,
        long amountCents,
        DateOnly accountingDate,
        string? description,
        TimeProvider clock)
    {
        if (merchantId == Guid.Empty)
        {
            throw new DomainRuleException("merchant_required", "The merchant is required.");
        }

        if (amountCents <= 0)
        {
            throw new DomainRuleException("invalid_amount", "The amount must be greater than zero.");
        }

        var now = clock.GetUtcNow();
        if (accountingDate > BusinessDate(now))
        {
            throw new DomainRuleException("future_accounting_date", "The accounting date cannot be in the future.");
        }

        if (description?.Length > MaxDescriptionLength)
        {
            throw new DomainRuleException("description_too_long", "The description cannot exceed 200 characters.");
        }

        return new LedgerEntry(Guid.CreateVersion7(now), merchantId, entryType, amountCents, accountingDate, description, null, now);
    }

    public static LedgerEntry Reverse(LedgerEntry original, TimeProvider clock)
    {
        if (original.ReversalOf is not null)
        {
            throw new DomainRuleException("reversal_not_allowed", "A reversal cannot be reversed.", RuleKind.Conflict);
        }

        var now = clock.GetUtcNow();
        var oppositeType = original.EntryType == EntryType.Credit ? EntryType.Debit : EntryType.Credit;

        return new LedgerEntry(
            Guid.CreateVersion7(now),
            original.MerchantId,
            oppositeType,
            original.AmountCents,
            original.AccountingDate,
            original.Description,
            original.Id,
            now);
    }

    private static DateOnly BusinessDate(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, BusinessTimeZone).DateTime);
}
