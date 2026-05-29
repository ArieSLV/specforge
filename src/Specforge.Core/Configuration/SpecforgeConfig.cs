namespace Specforge.Core.Configuration;

/// <summary>
/// Immutable, fully-validated, schema-upgraded view of a <c>.specforge.json</c> file
/// (DEC-002 schema, DEC-006 in-memory upgrade). The runtime always sees the current schema version.
/// </summary>
/// <param name="SchemaVersion">The in-memory schema version (always the binary's supported max after upgrade).</param>
/// <param name="Shared">Repo-level shared directory path, relative to the config file.</param>
/// <param name="Templates">Repo-level templates directory path, relative to the config file.</param>
/// <param name="Packages">Explicit package list (never globbed).</param>
/// <param name="ConfigPath">Absolute path to the source config file.</param>
public sealed record SpecforgeConfig(
    int SchemaVersion,
    string Shared,
    string Templates,
    IReadOnlyList<SpecforgePackageConfig> Packages,
    string ConfigPath);
