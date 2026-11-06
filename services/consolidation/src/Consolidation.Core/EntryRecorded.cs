// @anchors
//   code: CERCD
//   ref: APENT
//   updated_at: 2026-11-06
using System.Text.Json.Serialization;

namespace Consolidation.Core;

/// <summary>Starts at 1 so that a missing value is never a valid type.</summary>
[JsonConverter(typeof(EntryTypeJsonConverter))]
public enum EntryType
{
    [JsonStringEnumMemberName("CREDIT")]
    Credit = 1,

    [JsonStringEnumMemberName("DEBIT")]
    Debit = 2,
}

public sealed class EntryTypeJsonConverter() : JsonStringEnumConverter<EntryType>(allowIntegerValues: false);

public sealed class InvalidEventException(string message) : Exception(message);

/// <summary>This service's own reading of docs/contratos/entry-recorded.v1.schema.json.</summary>
public sealed record EntryRecorded(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid MerchantId,
    EntryType EntryType,
    long AmountCents,
    DateOnly AccountingDate);
