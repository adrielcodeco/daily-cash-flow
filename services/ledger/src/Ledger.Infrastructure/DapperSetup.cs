// @anchors
//   code: LDDAP
//   layer: infrastructure
//   updated_at: 2026-11-06
using System.Data;
using Dapper;

namespace Ledger.Infrastructure;

/// <summary>Dapper needs help with DateOnly, and column names use underscores.</summary>
internal static class DapperSetup
{
    private static readonly Lazy<bool> Configured = new(() =>
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new DateOnlyHandler());
        return true;
    });

    public static void Configure() => _ = Configured.Value;

    private sealed class DateOnlyHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value) => parameter.Value = value;

        public override DateOnly Parse(object value) =>
            value is DateTime dateTime ? DateOnly.FromDateTime(dateTime) : (DateOnly)value;
    }
}
