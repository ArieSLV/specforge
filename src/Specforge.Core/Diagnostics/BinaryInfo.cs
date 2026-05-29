using System.Reflection;

namespace Specforge.Core.Diagnostics;

/// <summary>
/// Exposes the binary's version and the schema-version range it supports (DEC-006).
/// Backs the <c>info</c> tool's diagnostic payload (DEC-007).
/// </summary>
public sealed class BinaryInfo
{
    /// <summary>Lowest schema version this binary can read.</summary>
    public const int SupportedSchemaVersionMin = 1;

    /// <summary>Highest schema version this binary can read (the in-memory upgrade target).</summary>
    public const int SupportedSchemaVersionMax = 1;

    public BinaryInfo()
    {
        Assembly assembly = typeof(BinaryInfo).Assembly;
        Version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";
    }

    /// <summary>Informational version string (may carry a commit SHA suffix on CI builds).</summary>
    public string Version { get; }

    /// <summary>Inclusive supported range as <c>[min, max]</c>.</summary>
    public IReadOnlyList<int> SupportedSchemaVersionRange => [SupportedSchemaVersionMin, SupportedSchemaVersionMax];
}
