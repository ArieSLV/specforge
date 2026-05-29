namespace Specforge.Core.Configuration.Migrations;

/// <summary>
/// DEC-006 in-memory upgrade chain entry for schema v1. v1 is the current version, so this is the
/// identity transform; the first real migration arrives with the chain's first <c>v1 → v2</c> step.
/// </summary>
public static class V1Migration
{
    /// <summary>Returns <paramref name="config"/> unchanged (v1 is the current schema version).</summary>
    public static SpecforgeConfig Apply(SpecforgeConfig config) => config;
}
