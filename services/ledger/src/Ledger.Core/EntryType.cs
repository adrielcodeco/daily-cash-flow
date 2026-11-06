// @anchors
//   code: ETYCD
//   ref: LEDEN
//   updated_at: 2026-11-05
using System.Text.Json.Serialization;

namespace Ledger.Core;

[JsonConverter(typeof(JsonStringEnumConverter<EntryType>))]
public enum EntryType
{
    [JsonStringEnumMemberName("CREDIT")]
    Credit,

    [JsonStringEnumMemberName("DEBIT")]
    Debit,
}
