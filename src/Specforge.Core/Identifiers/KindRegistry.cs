using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;

namespace Specforge.Core.Identifiers;

/// <summary>
/// Per-active-package knowledge of recognized kinds: the five core kinds plus the package's optional
/// <c>extraKinds</c> (DEC-004). Rebuilt per session; not cached across <c>use_package</c> calls.
/// </summary>
public sealed class KindRegistry
{
    private static readonly string[] CoreKindsArray = ["DEC", "ITEM", "ART", "REV", "CMT"];

    private readonly HashSet<string> _known;

    private KindRegistry(IEnumerable<string> extraKinds)
    {
        _known = new HashSet<string>(CoreKindsArray, StringComparer.Ordinal);
        foreach (string extra in extraKinds)
        {
            _known.Add(extra);
        }
    }

    /// <summary>The five reserved core kinds (DEC-004 §"Core Kinds").</summary>
    public static IReadOnlyList<string> CoreKinds => CoreKindsArray;

    /// <summary>Builds the registry for a named package in the loaded config.</summary>
    public static KindRegistry FromConfig(SpecforgeConfig config, string activePackageName)
    {
        ArgumentNullException.ThrowIfNull(config);
        SpecforgePackageConfig package = config.Packages.FirstOrDefault(p => string.Equals(p.Name, activePackageName, StringComparison.Ordinal))
            ?? throw new SpecforgeUnknownPackageException(activePackageName, config.Packages);
        return new KindRegistry(package.ExtraKinds);
    }

    /// <summary>A registry with only the core kinds (used when no package is selected).</summary>
    public static KindRegistry CoreOnly() => new([]);

    /// <summary>Builds the registry from the active session, or a core-only registry if nothing is selected.</summary>
    public static KindRegistry FromSession(SessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.Config is { } config && session.ActivePackageName is { } name
            ? FromConfig(config, name)
            : CoreOnly();
    }

    /// <summary>True if <paramref name="kind"/> is a core kind or one of the active package's extras.</summary>
    public bool IsKnownKind(string kind) => _known.Contains(kind);

    /// <summary>True if <paramref name="kind"/> is a reserved core kind.</summary>
    public static bool IsReservedKind(string kind) => CoreKindsArray.Contains(kind);

    /// <summary>Throws <see cref="SpecforgeReservedKindException"/> if any extra equals a core kind (DEC-004).</summary>
    public static void ValidateExtraKinds(IReadOnlyList<string> extraKinds)
    {
        ArgumentNullException.ThrowIfNull(extraKinds);
        foreach (string extra in extraKinds)
        {
            if (CoreKindsArray.Contains(extra))
            {
                throw new SpecforgeReservedKindException(extra, CoreKinds);
            }
        }
    }
}
