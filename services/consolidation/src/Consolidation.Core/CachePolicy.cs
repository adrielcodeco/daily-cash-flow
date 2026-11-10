// @anchors
//   code: CBPOL
//   ref: CBSTR
//   updated_at: 2026-11-09
namespace Consolidation.Core;

/// <summary>Which routes and tables use the cache. Empty means the cache is off.</summary>
public sealed class CachePolicy(IEnumerable<string> targets)
{
    private readonly HashSet<string> enabled = new(targets, StringComparer.OrdinalIgnoreCase);

    public static CachePolicy Parse(string? targets) =>
        new(targets?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? []);

    public bool Covers(string route, string table) => enabled.Contains(route) || enabled.Contains(table);
}
