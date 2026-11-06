// @anchors
//   code: CERCD
//   ref: APENT
//   updated_at: 2026-11-05
using System.Text.Json.Serialization;

namespace Consolidation.Core;

[JsonConverter(typeof(JsonStringEnumConverter<EntryType>))]
public enum EntryType
{
    [JsonStringEnumMemberName("CREDIT")]
    Credit,

    [JsonStringEnumMemberName("DEBIT")]
    Debit,
}

/// <summary>This service's own reading of docs/contratos/entry-recorded.v1.schema.json.</summary>
public sealed record EntryRecorded(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid MerchantId,
    EntryType EntryType,
    long AmountCents,
    DateOnly AccountingDate);
