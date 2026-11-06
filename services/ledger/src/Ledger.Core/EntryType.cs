// @anchors
//   code: ETYCD
//   ref: LEDEN
//   updated_at: 2026-11-06
using System.Text.Json.Serialization;

namespace Ledger.Core;

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
