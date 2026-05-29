using Specforge.Core.Configuration;

namespace Specforge.Core.Identifiers;

/// <summary>
/// Ambient context for <see cref="IdValidator"/>: the set of package names a qualified reference may
/// resolve against (DEC-004 §"Cross-Package Referencing").
/// </summary>
/// <param name="KnownPackages">Package names declared in the active config.</param>
public sealed record ValidationContext(IReadOnlyCollection<string> KnownPackages)
{
    /// <summary>A context with no known packages (qualified references will not resolve).</summary>
    public static ValidationContext Empty { get; } = new([]);

    /// <summary>Builds the context from a loaded config.</summary>
    public static ValidationContext FromConfig(SpecforgeConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return new ValidationContext([.. config.Packages.Select(p => p.Name)]);
    }
}
